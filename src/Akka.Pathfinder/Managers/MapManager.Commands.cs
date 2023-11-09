using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.States;
using Akka.Pathfinder.Core;
using Akka.Persistence;
using Akka.Actor;

namespace Akka.Pathfinder.Managers;

public partial class MapManager : ReceivePersistentActor
{
    private async Task LoadMapHandler(LoadMap msg)
    {
        _logger.Debug("[{ActorName}][{MessageType}] received", GetType().Name, msg.GetType().Name);
        Become(Waiting);

        _state = MapManagerState.FromRequest(msg);
        var pointCollectionIds = _mapConfigReader.Get(msg.MapId).PointConfigsIds;
        foreach (var collectionId in pointCollectionIds)
        {
            await _pointConfigReader.Get(collectionId)
            .Throttle(config =>
            {
                _pointWorker.Tell(new InitializePoint(config.Id, collectionId));
            }, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(15));
        }

        Sender.Tell(new MapLoaded(msg.MapId));
        _state.SetMapIsReady();
        Become(Ready);
    }

    private async Task UpdateMapHandler(UpdateMap msg)
    {
        _logger.Debug("[{ActorName}][{MessageType}] received", GetType().Name, msg.GetType().Name);
        _state = MapManagerState.FromRequest(msg);
        await Task.CompletedTask;
    }

    private async Task ResetMapHandler(ResetMap msg)
    {
        _logger.Debug("[{ActorName}][{MessageType}] received", GetType().Name, msg.GetType().Name);
        _state = MapManagerState.FromRequest(msg);
        await Task.CompletedTask;
    }

    private void FindPathRequestHandler(FindPathRequest msg)
    {
        _logger.Debug("[{ActorName}][{MessageType}] received", GetType().Name, msg.GetType().Name);
        if (_state.IsMapReady)
        {
            _pointWorker.Forward(msg);
            _pathfinderWorker.Tell(new FindPathRequestStarted(msg.PathfinderId));
        }

        Stash.Stash();
    }
}

