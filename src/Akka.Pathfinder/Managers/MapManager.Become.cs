using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Pathfinder.Core;
using Akka.Pathfinder.Core.Messages;
using Akka.Persistence;
using Akka.Streams;
using Akka.Streams.Dsl;
using moin.akka.endpoint;
using MongoDB.Driver.Linq;
using Servus.Akka.Diagnostics;
using Servus.Core.Diagnostics;

namespace Akka.Pathfinder.Managers;

public partial class MapManager
{
    private void Ready()
    {
        _logger.Information("[MapManager][READY]");
        CommandAsync<LoadMap>(LoadMapHandler);
        Command<GetMapState>(GetMapStateHandler);
        Command<FindPathRequest>(FindPathRequestHandler);
        DeleteCommands();
        CommandAny(msg => Stash.Stash());
        Stash.UnstashAll();
    }

    private void Waiting() => CommandAny(msg => Stash.Stash());

    private void DeleteCommands()
    {
        var deleteSender = ActorRefs.NoSender;
        DeleteMap request = null!;
        CommandAsync<DeleteMap>(async msg =>
        {
            using var activity = ActivitySourceRegistry.StartActivity(GetType(), msg.GetType().Name, msg);
            deleteSender = Sender;
            request = msg;
            var mapConfig = await _mapConfigWriter.GetAsync(msg.MapId);

            var startTime = DateTime.UtcNow;
            _ = await Source.From(mapConfig.CollectionIds)
                .SelectAsync(4,
                    async collectionId => await _pointConfigWriter.Get(collectionId).Select(x => x.Id).ToListAsync())
                .SelectMany(x => x.Select(pointId => new DeletePoint(pointId)).ToArray())
                .AskTraced<DeletePoint, PointDeleted, NotUsed>(_registry.GetClient<Endpoint.PointWorker>(), 128)
                .RunWith(Sink.Ignore<PointDeleted>(), Context.Materializer());
            var endTime = DateTime.UtcNow;

            _logger.Information("[{ActorName}][{MapId}] Map loaded TotalSeconds [{TotalSeconds}]", GetType().Name,
                msg.MapId,
                (endTime - startTime).TotalSeconds);

            foreach (var collectionId in mapConfig.CollectionIds)
            {
                await _pointConfigWriter.DeleteCollectionAsync(collectionId);
            }

            await _mapConfigWriter.DeleteAsync(msg.MapId);

            DeleteSnapshots(new SnapshotSelectionCriteria(SnapshotSequenceNr, DateTime.Now));
        });
        Command<DeleteSnapshotsSuccess>(msg =>
        {
            var response = new MapDeleted(request.RequestId, request.MapId, true);
            deleteSender?.TellTraced(response);
            Become(Ready);
        });
        Command<DeleteSnapshotFailure>(msg =>
        {
            var response = new MapDeleted(request.RequestId, request.MapId, false, msg.Cause);
            deleteSender?.TellTraced(response);
        });
    }
}