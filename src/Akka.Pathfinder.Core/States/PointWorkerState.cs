using System.Collections.Concurrent;
using System.Collections.Immutable;
using Akka.Pathfinder.Core.Configs;
using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.Persistence;
using Akka.Util.Internal;
using Path = Akka.Pathfinder.Core.Persistence.Data.Path;
using MongoDB.Driver.Linq;

namespace Akka.Pathfinder.Core.States;

public record PointWorkerState
{
    private readonly ConcurrentDictionary<Direction, DirectionConfig> _directionConfigs;
    private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<PointWorkerState>();
    private ConcurrentDictionary<Guid, DateTime> _inactivePathfinders = new();

    public static PointWorkerState FromSnapshot(PersistedPointWorkerState msg)
        => new(msg.DirectionConfigs)
        {
            PointId = msg.PointId,
            Cost = msg.Cost,
            State = msg.State,
            Initialize = true,
        };

    public static PointWorkerState FromConfig(PointConfig config, PointState? state)
        => new(config.DirectionConfigs)
        {
            PointId = config.Id,
            Cost = config.Cost,
            State = state ?? PointState.None,
            Initialize = true,
        };

    public PointWorkerState(IReadOnlyDictionary<Direction, DirectionConfig> configs)
    {
        _directionConfigs = new ConcurrentDictionary<Direction, DirectionConfig>(configs);
    }

    public int PointId { get; internal set; } = 0;

    public uint Cost { get; internal set; } = 0;

    public bool Initialize { get; internal set; }

    public bool IsBlocked => State is PointState.Blocked;

    public PointState State { get; internal set; }

    public IDictionary<Direction, DirectionConfig> DirectionConfigs => _directionConfigs;

    internal bool UpdatePointCost(PointCommit commit)
    {
        return ((ICommit)commit).ChangeMethod switch
        {
            ChangeMethod.Increase => (Cost += commit.AdditionalCost) == Cost,
            ChangeMethod.Decrease => (Cost -= commit.AdditionalCost) == Cost,
            _ or ChangeMethod.Invalid => false
        };
    }

    internal bool UpdateDirectionCost(DirectionCommit commit)
    {
        if (!_directionConfigs.TryGetValue(commit.Direction, out var directionConfig)) return false;

        static DirectionConfig Update(uint value, DirectionConfig config, ChangeMethod changeMethod) => changeMethod is ChangeMethod.Increase ? config.Increase(value) : config.Decrease(value);

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
            _ or Direction.None => throw new NotImplementedException(),
        };

        return _directionConfigs.TryUpdate(commit.Direction, newDirectionConfig, directionConfig);
    }

    public bool Block() => (State = PointState.Blocked) is PointState.Blocked;

    public bool Unblock() => (State = PointState.None) is PointState.None;

    public void AddInactivePathfinder(PathfinderDeactivated msg) => _inactivePathfinders.AddOrSet(msg.PathfinderId, DateTime.UtcNow);

    public void RemoveOldPathfinderIds(TimeSpan timeSpan) => _inactivePathfinders.RemoveAll((_, value) => value < DateTime.UtcNow.Add(-timeSpan));

    public bool IsBlockedAndGetResponse(FindPathRequest request, out PathFound response)
    {
        response = new PathFound(request.PathfinderId, request.PathId, PathFinderResult.PathBlocked);
        if (IsBlocked) return true;
        response = new PathFound(request.PathfinderId, request.PathId, PathFinderResult.MindBlown);
        if(_directionConfigs.Count == 0 && PointId != request.TargetPointId) return true;
        response = null!;
        return false;
    }

    public bool TryLoopDetection(FindPathRequest request, out PathFound response)
    {
        response = new PathFound(request.PathfinderId, request.PathId, PathFinderResult.LoopDetected);
        var loopDetectionList = request.Directions.SkipLast(request.Directions.Count).ToList();
        if (loopDetectionList.Any(x => x.PointId.Equals(PointId))) return true;
        response = null!;
        return false;
    }

    public bool TryIsInactivePathfinder(Guid pathfinderId) => _inactivePathfinders.ContainsKey(pathfinderId);

    public bool TryAddCurrentPointCost(FindPathRequest request, out FindPathRequest findPathRequest)
    {
        findPathRequest = request;
        if (request.Directions.Count == 1) return false;
        var pathList = request.Directions.ToConcurrentDictionary(x => x.PointId, x => x);
        var pointInfo = pathList.GetValueOrDefault(PointId);
        if (pointInfo is null) return true;
        pathList.AddOrUpdate(PointId, pointInfo with { Cost = Cost + pointInfo.Cost }, (key, old) => old with { Cost = Cost + old.Cost });
        findPathRequest = request with
        {
            Directions = pathList.Select(x => x.Value).ToList(),
        };

        return false;
    }

    public bool TryIsArrivedTargetPoint(FindPathRequest request, Func<Path, bool> writer, out PathFound response)
    {
        response = null!;
        if (!request.TargetPointId.Equals(PointId)) return false;
        var paths = request.Directions.ToList();
        var path = new Path(request.PathId, request.PathfinderId, (DateTimeOffset.UtcNow-request.PathfindingStarted).TotalMilliseconds, paths);
        var success = writer(path);
        if (!success)
        {
            _logger.Debug("[{PathId}][{PathfinderId}] update path failed", request.PathId, request.PathfinderId);
            response = new PathFound(request.PathfinderId, request.PathId, PathFinderResult.MindBlown);
            return true;
        }

        response = new PathFound(request.PathfinderId, request.PathId, PathFinderResult.Success);
        return true;
    }

    public IReadOnlyList<FindPathRequest> GetAllForwardMessages(FindPathRequest request)
    {
        var results = new List<FindPathRequest>();

        foreach (var (Key, Value) in _directionConfigs)
        {
            var directions = request.Directions.ToList();
            directions.Add(new PathPoint(Value.TargetPointId, Cost, Key));
            var findPathRequest = new FindPathRequest(request.PathfinderId, DateTimeOffset.UtcNow, Guid.NewGuid(), Value.TargetPointId, request.TargetPointId, directions);
            results.Add(findPathRequest);
        }

        return results;
    }

    public PersistedPointWorkerState GetPersistenceState() => new(PointId, Cost, _directionConfigs.AsReadOnly(), State);
}

public static class DictionaryExtensions
{
    public static void RemoveAll<TKey, TValue>(this IDictionary<TKey, TValue> dic, Func<TKey, TValue, bool> predicate)
        => dic.Where(pair => predicate(pair.Key, pair.Value)).ForEach(x => dic.Remove(x));
}