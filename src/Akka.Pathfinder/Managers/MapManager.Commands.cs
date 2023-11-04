using Akka.Actor;
using Akka.Pathfinder.Core;
using Akka.Pathfinder.Core.Messages;
using Akka.Persistence;
using Akka.Util.Internal;

namespace Akka.Pathfinder.Managers;

public partial class MapManager : ReceivePersistentActor
{
    public void LoadMapHandler(LoadMap msg)
    {
        _logger.Debug("[{ActorName}][{MessageType}] received", GetType().GetEnumName, msg.GetType().Name);
        using var scope = _serviceScopeFactory.CreateScope();
        var mapConfigReader = scope.ServiceProvider.GetRequiredService<IMapConfigReader>();
        Become(WaitingForPoints);
        mapConfigReader.Get(msg.MapId).ForEach(x =>
        {
            _pointWorkerClient.Tell(new InitializePoint(x));
            _readyPoints.AddOrUpdate(x.Id, _ => (DateTime.UtcNow, null), (_, _) => (DateTime.UtcNow, null));
        });
    }

    public void UpdateMapHandler(UpdateMap msg)
    {
        _logger.Debug("[{ActorName}][{MessageType}] received", GetType().GetEnumName, msg.GetType().Name);

    }

    public void ResetMapHandler(ResetMap msg)
    {
        _logger.Debug("[{ActorName}][{MessageType}] received", GetType().GetEnumName, msg.GetType().Name);

    }

    public void IsMapReadyHandler(IsMapReady msg)
    {
        _logger.Debug("[{ActorName}][{MessageType}] received", GetType().GetEnumName, msg.GetType().Name);
        _waitingPathfinders.Add(msg.PathFinderId);

        var isReady = _readyPoints.All(x => x.Value.Completed.HasValue);


        PathFinderRequest response = isReady ? new MapIsReady(msg.PathFinderId) : new MapIsNotReady(msg.PathFinderId);
        Sender.Tell(response);
    }

    public void AllPointsInitializedHandler(AllPointsInitialized msg)
    {

    }

    public void NotAllPointsInitializedHandler(NotAllPointsInitialized _)
    {
        _logger.Debug("[{ActorName}] not all points initialized", GetType().Name);
    }

    public async Task PointInitializedHandler(PointInitialized msg)
    {
        _logger.Debug("[{ActorName}][{MessageType}] received", GetType().GetEnumName, msg.GetType().Name);
        if (!_readyPoints.TryGetValue(msg.PointId, out var oldValue) && !_readyPoints.TryUpdate(msg.PointId, (oldValue.Created, DateTime.UtcNow), oldValue))
        {
            _logger.Debug("[{ActorName}] Unkown point initialized - PointId:[{PointId}]", GetType().GetEnumName, msg.PointId);
            _readyPoints.AddOrSet(msg.PointId, (DateTime.UtcNow, DateTime.UtcNow));
        }

        await _readyPoints
        .ToAsyncEnumerable()
        .AllAsync(x => x.Value.Completed.HasValue)
        .PipeTo(Self, Self, x => x ? new AllPointsInitialized() : new NotAllPointsInitialized());
    }
}

