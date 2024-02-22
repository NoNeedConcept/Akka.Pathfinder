using Akka.Actor;
using Akka.Pathfinder.Core;
using Akka.Pathfinder.Core.Messages;
using Akka.Persistence;
using Akka.Streams;
using Akka.Streams.Dsl;
using moin.akka.endpoint;
using MongoDB.Driver.Linq;
using Servus.Akka.Diagnostics;
using Servus.Core.Diagnostics;
using Endpoint = Akka.Pathfinder.Core.Endpoint;

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
            
            var parentTraceId = activity?.TraceId.ToHexString();
            var parentSpanId = activity?.SpanId.ToHexString();

            var startTime = DateTime.UtcNow;
            _ = await Source.From(mapConfig.CollectionIds)
                .SelectAsync(4,
                    async collectionId => await _pointConfigWriter.Get(collectionId).Select(x => x.Id).ToListAsync())
                .SelectMany(x => x.Select(pointId => new DeletePoint(pointId).WithTracing(parentTraceId, parentSpanId)).ToArray())
                .Ask<DeletePoint, PointDeleted, NotUsed>(_registry.GetClient<Endpoint.PointWorker>(), 128)
                .RunWith(Sink.Ignore<PointDeleted>(), Context.Materializer());
            var endTime = DateTime.UtcNow;

            _logger.Information("[{ActorName}][{MapId}] Map deleted TotalSeconds [{TotalSeconds}]", GetType().Name,
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
            var response = new MapDeleted(request.RequestId, request.MapId, true)
            {
                TraceId = request.TraceId,
                SpanId = request.SpanId
            };
            deleteSender?.TellTraced(response);
            Become(Ready);
        });
        Command<DeleteSnapshotFailure>(msg =>
        {
            var response = new MapDeleted(request.RequestId, request.MapId, false, msg.Cause)
            {
                TraceId = request.TraceId,
                SpanId = request.SpanId
            };
            deleteSender?.TellTraced(response);
        });
    }
}