using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.States;
using Akka.Pathfinder.Core;
using Akka.Util.Internal;
using Akka.Persistence;
using Akka.Actor;

namespace Akka.Pathfinder.Managers;

public partial class MapManager : ReceivePersistentActor
{
    public async Task LoadMapHandler(LoadMap msg)
    {
        _logger.Debug("[{ActorName}][{MessageType}] received", GetType().Name, msg.GetType().Name);
        Become(WaitingForPoints);
        using var scope = _serviceScopeFactory.CreateScope();
        var mapConfigReader = scope.ServiceProvider.GetRequiredService<IMapConfigReader>();
        _state = MapManagerState.FromRequest(msg, _state.GetWaitingPathfinders());
        var client = Context.System.GetRegistry().Get<PointWorkerProxy>();
        await mapConfigReader.Get(msg.MapId).Throttle(x =>
        {
            client.Tell(new InitializePoint(x));
            _state.Add(x.Id);
        }, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(15));
        Sender.Tell(new MapLoaded(msg.MapId));
    }

    public void UpdateMapHandler(UpdateMap msg)
    {
        _logger.Debug("[{ActorName}][{MessageType}] received", GetType().Name, msg.GetType().Name);
        using var scope = _serviceScopeFactory.CreateScope();
        var mapConfigReader = scope.ServiceProvider.GetRequiredService<IMapConfigReader>();
        _state = MapManagerState.FromRequest(msg, _state.GetWaitingPathfinders());
        Become(WaitingForPoints);
        mapConfigReader.GetPointWithChanges(msg.MapId).ForEach(x =>
        {
            var client = Context.System.GetRegistry().Get<PointWorkerProxy>();
            client.Tell(new UpdatePointDirection(x));
            _state.Add(x.Id);
        });
    }

    public void ResetMapHandler(ResetMap msg)
    {
        _logger.Debug("[{ActorName}][{MessageType}] received", GetType().Name, msg.GetType().Name);
        using var scope = _serviceScopeFactory.CreateScope();
        var mapConfigReader = scope.ServiceProvider.GetRequiredService<IMapConfigReader>();
        _state = MapManagerState.FromRequest(msg, _state.GetWaitingPathfinders());
        Become(WaitingForPoints);
        mapConfigReader.Get(msg.MapId).ForEach(x =>
        {
            var client = Context.System.GetRegistry().Get<PointWorkerProxy>();
            client.Tell(new ResetPoint(x));
            _state.Add(x.Id);
        });
    }

    public void IsMapReadyHandler(IsMapReady msg)
    {
        _logger.Debug("[{ActorName}][{MessageType}] received", GetType().Name, msg.GetType().Name);
        if (_state.IsMapReady)
        {
            Sender.Tell(new MapIsReady(msg.PathFinderId));
        }

        _state.AddWaitingPathfinder(msg.PathFinderId);
    }

    public void AllPointsInitializedHandler(AllPointsInitialized msg)
    {
        _logger.Debug("[{ActorName}][{MessageType}] received", GetType().Name, msg.GetType().Name);

        _state
        .GetMapIsReadyMessages()
        .ForEach(x =>
        {
            var client = Context.System.GetRegistry().Get<PathfinderProxy>();
            client.Tell(x);
        });

        Become(Ready);
    }

    public void NotAllPointsInitializedHandler(NotAllPointsInitialized msg)
    {
        _logger.Debug("[{ActorName}][{MessageType}] received", GetType().Name, msg.GetType().Name);
    }

    public void PointInitializedHandler(PointInitialized msg)
    {
        _logger.Debug("[{ActorName}][{MessageType}] received", GetType().Name, msg.GetType().Name);
        
        _state.Remove(msg.PointId);

        if (_state.AllPointsReady)
        {
            Sender.Tell(new AllPointsInitialized());
        }
    }
}

