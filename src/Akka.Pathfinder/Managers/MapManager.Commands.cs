using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.States;
using Akka.Actor;
using Akka.Streams.Dsl;
using Akka.Streams;
using moin.akka.endpoint;
using MongoDB.Driver.Linq;

namespace Akka.Pathfinder.Managers;

public partial class MapManager
{
    private void GetMapStateHandler(GetMapState msg)
    {
        var mapId = _state.MapId != msg.MapId ? Guid.Empty : msg.MapId;
        Sender.Tell(new MapStateResponse(msg.RequestId, mapId, _state.IsMapReady));
    }

    private async Task LoadMapHandler(LoadMap msg)
    {
        using var activity = Telemetry.ActivitySource.StartActivity(msg.GetType().Name);
        _logger.Verbose("[{ActorName}][{MessageType}] received", GetType().Name, msg.GetType().Name);
        Become(Waiting);

        _state = MapManagerState.FromRequest(msg.MapId);
        var mapConfig = await _mapConfigReader.GetAsync(msg.MapId);

        var startTime = DateTime.UtcNow;
        _ = await Source.From(mapConfig.CollectionIds)
            .SelectAsync(4,
                async collectionId => Tuple.Create(collectionId,
                    await _pointConfigReader.Get(collectionId).Select(x => x.Id).ToListAsync()))
            .SelectMany(tuple => tuple.Item2.Select(pointId => new InitializePoint(pointId, tuple.Item1)).ToArray())
            .Ask<PointInitialized>(_registry.GetClient<Endpoint.PointWorker>(), TimeSpan.FromSeconds(15), 128)
            .RunWith(Sink.Ignore<PointInitialized>(), Context.Materializer());
        var endTime = DateTime.UtcNow;

        _logger.Information("[{ActorName}][{MapId}] Maploaded TotalSeconds [{TotalSeconds}]", GetType().Name, msg.MapId,
            (endTime - startTime).TotalSeconds);
        _state.SetMapIsReady();
        Sender.Tell(new MapLoaded(msg.RequestId, msg.MapId));
        Become(Ready);
    }

    private void UpdateMapHandler(UpdateMap msg)
    {
        using var activity = Telemetry.ActivitySource.StartActivity(msg.GetType().Name);
        _logger.Verbose("[{ActorName}][{MessageType}] received", GetType().Name, msg.GetType().Name);
        _state = MapManagerState.FromRequest(msg.MapId);
        Sender.Tell(new MapUpdated(msg.RequestId, msg.MapId));
    }

    private void FindPathRequestHandler(FindPathRequest msg)
    {
        using var activity = Telemetry.ActivitySource.StartActivity(msg.GetType().Name);
        _logger.Verbose("[{ActorName}][{MessageType}] received", GetType().Name, msg.GetType().Name);
        if (!_state.IsMapReady)
        {
            Stash.Stash();
            return;
        }

        _registry.GetClient<Endpoint.PointWorker>().Forward(msg);
    }
}