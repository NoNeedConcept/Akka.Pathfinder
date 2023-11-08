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

    public bool IsMapReady { get; internal set; } = false;

    public bool AllPointsReady => _readyPoints.IsEmpty;

    public IDictionary<Guid, Guid> GetWaitingPathfinders() => _waitingPathfinders;
    public void Add(int pointId) => _readyPoints.AddOrSet(pointId, (DateTime.UtcNow, null));
    public void Remove(int pointId)
    {
        _readyPoints.Remove(pointId, out _);
        _logger.Debug("NotReadyPoints: [{Count}]", _readyPoints.Count);
    }

    public void AddWaitingPathfinder(Guid pathfinderId) => _waitingPathfinders.AddOrSet(pathfinderId, pathfinderId);

    public List<MapIsReady> GetMapIsReadyMessages()
    {
        var result = _waitingPathfinders.Select(x => new MapIsReady(x.Key)).ToList();
        _waitingPathfinders = new();
        IsMapReady = true;
        return result;
    }
}
