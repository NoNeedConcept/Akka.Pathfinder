using System.Collections.Immutable;
using Akka.Pathfinder.Core.Configs;
using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.Persistence;
using Akka.Util.Internal;
using Path = Akka.Pathfinder.Core.Persistence.Data.Path;
using Akka.Pathfinder.Core.Persistence.Data;

namespace Akka.Pathfinder.Core.States;

public record PointWorkerState
{
    private readonly Dictionary<Direction, DirectionConfig> _directionConfigs;
    private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<PointWorkerState>();
    private readonly Dictionary<Guid, DateTime> _inactivePathfinders = [];
    private readonly Dictionary<Guid, int> _pathfinderPathCost = [];

    public static PointWorkerState FromInitialize(int pointId, Guid collectionId)
        => new(new Dictionary<Direction, DirectionConfig>())
        {
            PointId = pointId,
            CollectionId = collectionId,
            Initialize = true
        };

    public static PointWorkerState FromSnapshot(PersistedPointWorkerState msg)
        => new(msg.DirectionConfigs)
        {
            PointId = msg.PointId,
            Cost = msg.Cost,
            State = msg.State,
            Loaded = msg.Loaded,
            Initialize = true
        };

    public static PointWorkerState FromPersistedPointState(PersistedInitializedPointState state)
        => new(new Dictionary<Direction, DirectionConfig>())
        {
            PointId = state.PointId,
            CollectionId = state.CollectionId,
            Initialize = true
        };

    public static PointWorkerState FromConfig(PointConfig config, PointState? state)
        => new(config.DirectionConfigs)
        {
            PointId = config.Id,
            Cost = config.Cost,
            State = state ?? PointState.None,
            Initialize = true,
            Loaded = true
        };

    public PointWorkerState(IReadOnlyDictionary<Direction, DirectionConfig> configs)
    {
        _directionConfigs = new Dictionary<Direction, DirectionConfig>(configs);
    }

    public int PointId { get; private set; } = 0;

    public Guid CollectionId { get; private set; }

    public uint Cost { get; private set; } = 0;

    public bool Initialize { get; private set; }

    public bool Loaded { get; private set; }

    public bool IsBlocked => State is PointState.Blocked;

    public PointState State { get; private set; }

    public IDictionary<Direction, DirectionConfig> DirectionConfigs => _directionConfigs;

    internal bool UpdatePointCost(PointCommit commit)
    {
        return ((ICommit)commit).ChangeMethod switch
        {
            ChangeMethod.Increase => (Cost += commit.AdditionalCost) == Cost,
            ChangeMethod.Decrease => (Cost -= commit.AdditionalCost) == Cost,
            _ => false
        };
    }

    internal bool UpdateDirectionCost(DirectionCommit commit)
    {
        if (!_directionConfigs.TryGetValue(commit.Direction, out var directionConfig)) return false;

        var changeMethod = ((ICommit)commit).ChangeMethod;
        var newDirectionConfig = commit.Direction switch
        {
            Direction.Top => Update(commit.AdditionalCost, directionConfig, changeMethod),
            Direction.Bottom => Update(commit.AdditionalCost, directionConfig, changeMethod),
            Direction.Left => Update(commit.AdditionalCost, directionConfig, changeMethod),
            Direction.Right => Update(commit.AdditionalCost, directionConfig, changeMethod),
            Direction.Front => Update(commit.AdditionalCost, directionConfig, changeMethod),
            Direction.Back => Update(commit.AdditionalCost, directionConfig, changeMethod),
            Direction.All => throw new NotImplementedException(), // Hier muss noch was gemacht werden aber ich weiss leider noch nicht wie!! @HolySafe
            _ => throw new NotImplementedException(),
        };

        _directionConfigs.Remove(commit.Direction);
        return _directionConfigs.TryAdd(commit.Direction, newDirectionConfig);

        static DirectionConfig Update(uint value, DirectionConfig config, ChangeMethod changeMethod) => changeMethod is ChangeMethod.Increase ? config.Increase(value) : config.Decrease(value);
    }

    public bool Block() => (State = PointState.Blocked) is PointState.Blocked;

    public bool Unblock() => (State = PointState.None) is PointState.None;

    public void AddPathfinderPathCost(Guid pathfinderId, int cost) => _pathfinderPathCost[pathfinderId] = cost;

    public void AddInactivePathfinder(Guid pathfinderId) => _inactivePathfinders[pathfinderId] = DateTime.UtcNow;

    public void RemovePathfinderPathCost(Guid pathfinderId) => _pathfinderPathCost.Remove(pathfinderId, out _);

    public void RemoveOldPathfinderIds(TimeSpan timeSpan) => _inactivePathfinders.RemoveAll((_, value) => value < DateTime.UtcNow.Add(-timeSpan));

    public bool IsBlockedAndGetResponse(FindPathRequest request, out PathFound response)
    {
        response = new PathFound(request.RequestId, request.PathfinderId, request.PathId, PathfinderResult.PathBlocked);
        if (IsBlocked) return true;
        response = new PathFound(request.RequestId, request.PathfinderId, request.PathId, PathfinderResult.Unknown);
        if (_directionConfigs.Count == 0 && PointId != request.TargetPointId) return true;
        response = null!;
        return false;
    }

    public bool TryLoopDetection(FindPathRequest request)
    {
        var loopDetectionList = request.Directions.SkipLast(1);
        return loopDetectionList.Any(x => x.PointId.Equals(PointId));
    }

    public bool TryIsInactivePathfinder(Guid pathfinderId) => _inactivePathfinders.ContainsKey(pathfinderId);

    public bool TryAddCurrentPointCost(FindPathRequest request, out FindPathRequest findPathRequest)
    {
        findPathRequest = request;
        if (request.Directions.Count == 1) return false;
        var pathList = request.Directions.ToDictionary(x => x.PointId);
        var pointInfo = pathList.GetValueOrDefault(PointId);
        if (pointInfo is null) return true;
        pathList[PointId] = pointInfo with { Cost = Cost + pointInfo.Cost };
        findPathRequest = request with
        {
            Directions = pathList.Select(x => x.Value).ToImmutableList(),
        };

        return false;
    }

    public bool TryIsArrivedTargetPoint(FindPathRequest request, Func<Path, bool> writer, out PathFound response)
    {
        response = null!;
        if (!request.TargetPointId.Equals(PointId)) return false;
        var paths = request.Directions.ToImmutableList();
        var path = new Path(request.PathId, request.PathfinderId, request.RequestId, paths);
        var success = writer(path);
        if (!success)
        {
            _logger.Debug("[{PathId}][{PathfinderId}] update path failed", request.PathId, request.PathfinderId);
            response = new PathFound(request.RequestId, request.PathfinderId, request.PathId, PathfinderResult.Unknown);
            return true;
        }

        response = new PathFound(request.RequestId, request.PathfinderId, request.PathId, PathfinderResult.Success);
        return true;
    }

    public bool TryIsNotShortestPathForPathfinderId(FindPathRequest reqeust)
    {
        if (reqeust.TargetPointId.Equals(PointId)) return false;
        var currentPathCost = reqeust.Directions.Select(x => x.Cost).Sum(x => (int)x);
        if (_pathfinderPathCost.TryGetValue(reqeust.PathfinderId, out var value) && value <= currentPathCost) return true;
        _pathfinderPathCost[reqeust.PathfinderId] = currentPathCost;
        return false;
    }

    public IEnumerable<FindPathRequest> GetAllForwardMessages(FindPathRequest request)
    {
        var infoIds = request.Directions
        .GroupJoin(_directionConfigs, x => x.PointId, x => x.Value.TargetPointId, (info, points) => points.Any() ? info.PointId : int.MinValue)
        .Where(x => x != int.MinValue);

        foreach (var (key, value) in _directionConfigs.ExceptBy(infoIds, x => x.Value.TargetPointId))
        {
            var directions = request.Directions.AsEnumerable().Append(new PathPoint(value.TargetPointId, value.Cost, key)).ToImmutableList();
            yield return new FindPathRequest(request.RequestId, request.PathfinderId, Guid.NewGuid(), value.TargetPointId, request.TargetPointId, directions);
        }
    }

    public PersistedPointWorkerState GetPersistenceState() => new(PointId, CollectionId, Cost, _directionConfigs.ToDictionary(), State, Loaded);

    private static Direction Invert(Direction direction) => direction switch
    {
        Direction.Back => Direction.Front,
        Direction.Front => Direction.Back,
        Direction.Bottom => Direction.Top,
        Direction.Top => Direction.Bottom,
        Direction.Left => Direction.Right,
        Direction.Right => Direction.Left,
        Direction.None => Direction.None,
        _ => throw new InvalidOperationException("KEKW")
    };
}

public static class DictionaryExtensions
{
    public static void RemoveAll<TKey, TValue>(this IDictionary<TKey, TValue> dic, Func<TKey, TValue, bool> predicate)
        => dic.Where(pair => predicate(pair.Key, pair.Value)).ForEach(x => dic.Remove(x));
}