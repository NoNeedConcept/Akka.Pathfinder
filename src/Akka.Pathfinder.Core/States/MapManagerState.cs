using System.Collections.Concurrent;
using Akka.Pathfinder.Core.Messages;
using Akka.Util.Internal;

namespace Akka.Pathfinder.Core.States;

public class MapManagerState
{
    private readonly ConcurrentDictionary<int, (DateTime Created, DateTime? Completed)> _readyPoints = new();
    private ConcurrentDictionary<Guid, Guid> _waitingPathfinders = new();
    private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<MapManagerState>();

    public static MapManagerState FromRequest(LoadMap request, IDictionary<Guid, Guid> waitingPathfinders)
        => new(waitingPathfinders)
        {
            IsMapReady = false
        };

    public static MapManagerState FromRequest(UpdateMap request, IDictionary<Guid, Guid> waitingPathfinders)
        => new(waitingPathfinders)
        {
            IsMapReady = false
        };

    public static MapManagerState FromRequest(ResetMap request, IDictionary<Guid, Guid> waitingPathfinders)
        => new(waitingPathfinders)
        {
            IsMapReady = false
        };

    public MapManagerState(IDictionary<Guid, Guid> waitingPathfinders) => _waitingPathfinders = waitingPathfinders.ToConcurrentDictionary(x => x.Key, x => x.Value);

    public bool IsMapReady { get; init; } = false;

    public IDictionary<Guid, Guid> GetWaitingPathfinders() => _waitingPathfinders;
    public void AddInitializePoint(int pointId) => _readyPoints.AddOrSet(pointId, (DateTime.UtcNow, null));
    public void UpdatePointInitialized(int pointId)
    {
        if (_readyPoints.TryGetValue(pointId, out var oldValue))
        {
            _readyPoints.AddOrUpdate(pointId, (_) => (oldValue.Created, DateTime.UtcNow), (_, _) => (oldValue.Created, DateTime.UtcNow));
        }
        else
        {
            _logger.Debug("[{StateName}] Unkown point initialized - PointId:[{PointId}]", GetType().Name, pointId);
            _readyPoints.AddOrSet(pointId, (DateTime.UtcNow, DateTime.UtcNow));
        }
    }

    public void AddWaitingPathfinder(Guid pathfinderId) => _waitingPathfinders.AddOrSet(pathfinderId, pathfinderId);

    public List<MapIsReady> GetMapIsReadyMessages()
    {
        var result = _waitingPathfinders.Select(x => new MapIsReady(x.Key)).ToList();
        _waitingPathfinders = new();
        return result;
    }

    public async Task<bool> AllPointsReadyAsync()
    {
        var result = _readyPoints.AsEnumerable().All(x => x.Value.Completed.HasValue);
        await Task.CompletedTask;
        return result;
    }
}
