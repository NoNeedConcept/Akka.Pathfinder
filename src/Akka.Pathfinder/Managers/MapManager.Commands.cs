using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.States;
using Akka.Actor;
using Akka.Pathfinder.Core;
using Akka.Streams.Dsl;
using Akka.Streams;
using moin.akka.endpoint;
using MongoDB.Driver.Linq;
using Servus.Akka.Diagnostics;
using Servus.Core.Diagnostics;

namespace Akka.Pathfinder.Managers;

public partial class MapManager
{
    private void GetMapStateHandler(GetMapState msg)
    {
        using var activity = ActivitySourceRegistry.StartActivity(GetType(), msg.GetType().Name, msg);
        var mapId = _state.MapId != msg.MapId ? Guid.Empty : msg.MapId;
        Sender.TellTraced(new MapStateResponse(msg.RequestId, mapId, _state.IsMapReady));
    }

    private async Task LoadMapHandler(LoadMap msg)
    {
        using var activity = ActivitySourceRegistry.StartActivity(GetType(), msg.GetType().Name, msg);
        _logger.Verbose("[{ActorName}][{MessageType}] received", GetType().Name, msg.GetType().Name);
        Become(Waiting);

        _state = MapManagerState.FromRequest(msg.MapId);
        var mapConfig = await _mapConfigWriter.GetAsync(msg.MapId);

        var startTime = DateTime.UtcNow;
        _ = await Source.From(mapConfig.CollectionIds)
            .SelectAsync(4,
                async collectionId => Tuple.Create(collectionId,
                    await _pointConfigWriter.Get(collectionId).Select(x => x.Id).ToListAsync()))
            .SelectMany(tuple => tuple.Item2.Select(pointId => new InitializePoint(pointId, tuple.Item1)).ToArray())
            .AskTraced<InitializePoint, PointInitialized, NotUsed>(_registry.GetClient<Endpoint.PointWorker>(), 128)
            .RunWith(Sink.Ignore<PointInitialized>(), Context.Materializer());
        var endTime = DateTime.UtcNow;

        _logger.Information("[{ActorName}][{MapId}] Map loaded TotalSeconds [{TotalSeconds}]", GetType().Name, msg.MapId,
            (endTime - startTime).TotalSeconds);
        _state.SetMapIsReady();
        Sender.TellTraced(new MapLoaded(msg.RequestId, msg.MapId));
        PersistState();
        Become(Ready);
    }

    private void FindPathRequestHandler(FindPathRequest msg)
    {
        using var activity = ActivitySourceRegistry.StartActivity(GetType(), msg.GetType().Name, msg);
        _logger.Verbose("[{ActorName}][{MessageType}] received", GetType().Name, msg.GetType().Name);
        if (!_state.IsMapReady)
        {
            Stash.Stash();
            return;
        }

        _registry.GetClient<Endpoint.PointWorker>().ForwardTraced(msg);
    }
}