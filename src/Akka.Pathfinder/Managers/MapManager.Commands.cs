using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.States;
using Akka.Actor;
using Akka.Streams.Dsl;
using MongoDB.Driver;
using Akka.Streams;
using LinqToDB;
using Akka.Pathfinder.Core;

namespace Akka.Pathfinder.Managers;

public partial class MapManager
{
    private async Task LoadMapHandler(LoadMap msg)
    {
        _logger.Verbose("[{ActorName}][{MessageType}] received", GetType().Name, msg.GetType().Name);
        Become(Waiting);

        _state = MapManagerState.FromRequest(msg.MapId);
        var mapConfig = await _mapConfigReader.GetAsync(msg.MapId);

        var startTime = DateTime.UtcNow;
        _ = await Source.From(mapConfig.CollectionIds)
        .SelectMany(collectionId => _pointConfigReader.Get(collectionId).Select(x => x.Id).ToList().Select(pointId => Tuple.Create(pointId, collectionId)))
        .SelectAsync(32, x => Task.FromResult(new InitializePoint(x.Item1, x.Item2)))
        .Buffer(256, OverflowStrategy.Backpressure)
        .Ask<PointInitialized>(Context.GetRegistry().Get<PointWorkerProxy>(), TimeSpan.FromSeconds(15), 32)
        .RunWith(Sink.Ignore<PointInitialized>(), Context.Materializer());
        var endTime = DateTime.UtcNow;

        _logger.Information("[{ActorName}][{MapId}] Maploaded TotalSeconds [{TotalSeconds}]", GetType().Name, msg.MapId, (endTime - startTime).TotalSeconds);
        _state.SetMapIsReady();
        Sender.Tell(new MapLoaded(msg.RequestId, msg.MapId));
        Become(Ready);
    }

    private async Task UpdateMapHandler(UpdateMap msg)
    {
        _logger.Verbose("[{ActorName}][{MessageType}] received", GetType().Name, msg.GetType().Name);
        _state = MapManagerState.FromRequest(msg.MapId);
        await Task.CompletedTask;
        Sender.Tell(new MapUpdated(msg.RequestId, msg.MapId));
    }

    private void FindPathRequestHandler(FindPathRequest msg)
    {
        _logger.Verbose("[{ActorName}][{MessageType}] received", GetType().Name, msg.GetType().Name);
        if (!_state.IsMapReady)
        {
            Stash.Stash();
            return;
        }

        Context.GetRegistry().Get<PointWorkerProxy>().Forward(msg);
    }
}

