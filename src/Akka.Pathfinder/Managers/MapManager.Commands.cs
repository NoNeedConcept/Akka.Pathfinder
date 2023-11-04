using Akka.Actor;
using Akka.Pathfinder.Core;
using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.States;
using Akka.Persistence;
using Akka.Util.Internal;

namespace Akka.Pathfinder.Managers;

public partial class MapManager : ReceivePersistentActor
{
    public void LoadMapHandler(LoadMap msg)
    {
        _logger.Debug("[{ActorName}][{MessageType}] received", GetType().Name, msg.GetType().Name);
        Become(WaitingForPoints);
        using var scope = _serviceScopeFactory.CreateScope();
        var mapConfigReader = scope.ServiceProvider.GetRequiredService<IMapConfigReader>();
        _state = MapManagerState.FromRequest(msg, _state.GetWaitingPathfinders());
        mapConfigReader.Get(msg.MapId).ForEach(x =>
        {
            var client = Context.System.GetRegistry().Get<PointWorkerProxy>();
            client.Tell(new InitializePoint(x));
            _state.Add(x.Id);
        });
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

    public async Task PointInitializedHandler(PointInitialized msg)
    {
        _logger.Debug("[{ActorName}][{MessageType}] received", GetType().Name, msg.GetType().Name);

        _state.Complete(msg.PointId);

        await _state
        .AllPointsReadyAsync()
        .PipeTo(Self, Self, x => x ? new AllPointsInitialized() : new NotAllPointsInitialized());
    }
}

