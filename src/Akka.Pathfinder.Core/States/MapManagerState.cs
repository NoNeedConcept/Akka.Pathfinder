using System.Collections.Concurrent;
using Akka.Pathfinder.Core.Messages;
using Akka.Util.Internal;

namespace Akka.Pathfinder.Core.States;

public class MapManagerState
{
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

    public IDictionary<Guid, Guid> GetWaitingPathfinders() => _waitingPathfinders;

    public void AddWaitingPathfinder(Guid pathfinderId) => _waitingPathfinders.AddOrSet(pathfinderId, pathfinderId);

    public List<MapIsReady> GetMapIsReadyMessages()
    {
        var result = _waitingPathfinders.Select(x => new MapIsReady(x.Key)).ToList();
        _waitingPathfinders = new();
        IsMapReady = true;
        return result;
    }
}
