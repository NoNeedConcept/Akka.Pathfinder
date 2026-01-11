using Akka.Pathfinder.Core.Messages;
using Akka.Persistence;
using Akka.Actor;
using Servus.Akka.Diagnostics;
using Servus.Core.Diagnostics;

namespace Akka.Pathfinder.Workers;

public partial class PointWorker
{
    private void Initialize()
    {
        _logger.Verbose("[{PointId}][INITIALIZE]", _entityId);
        Command<InitializePoint>(InitializePointHandler);
        DeleteCommands();
    }

    private void Configure()
    {
        _logger.Verbose("[{PointId}][CONFIGURE]", _entityId);
        Command<LocalPointConfig>(LocalPointConfigHandler);
        CommandAny(msg => Stash.Stash());
        OnConfigure();
    }

    private void Update()
    {
        _logger.Verbose("[{PointId}][UPDATE]", _entityId);
        Command<LocalPointConfig>(LocalPointConfigHandler);
    }

    private void Failure()
    {
        _logger.Warning("[{PointId}][FAILURE]", _entityId);
        DeleteCommands();
    }

    private void Ready()
    {
        Stash.UnstashAll();
        _logger.Verbose("[{PointId}][READY]", _entityId);
        // Sender -> PathfinderWorker
        Command<FindPathRequest>(FindPathRequestHandler);
        Command<PathfinderDeactivated>(PathfinderDeactivatedHandler);
        // Sender -> MapManager 
        Command<CostRequest>(CostRequestHandler);
        Command<PointCommandRequest>(PointCommandRequestHandler);
        Command<InitializePoint>(msg =>
        {
            using var activity = ActivitySourceRegistry.StartActivity(GetType(), msg.GetType().Name, msg);
            Sender.TellTraced(new PointInitialized(msg.RequestId, msg.PointId));
        });
        Command<UpdatePointDirection>(UpdatePointDirectionHandler);
        // Sender -> SnapshotStore
        Command<SaveSnapshotSuccess>(SaveSnapshotSuccessHandler);
        Command<SaveSnapshotFailure>(SaveSnapshotFailureHandler);

        DeleteCommands();
    }

    private void DeleteCommands()
    {
        var deleteSender = ActorRefs.NoSender;
        DeletePoint request = null!;
        Command<DeletePoint>(msg =>
        {
            deleteSender = Sender;
            request = msg;
            DeleteSnapshots(new SnapshotSelectionCriteria(SnapshotSequenceNr, DateTime.Now));
        });
        Command<DeleteSnapshotsSuccess>(msg =>
        {
            var response = new PointDeleted(request.RequestId, request.PointId, true)
            {
                TraceId = request.TraceId,
                SpanId = request.SpanId
            };
            deleteSender?.TellTraced(response);
            Become(Initialize);
        });
        Command<DeleteSnapshotFailure>(msg =>
        {
            var response = new PointDeleted(request.RequestId, request.PointId, false, msg.Cause)
            {
                TraceId = request.TraceId,
                SpanId = request.SpanId
            };
            deleteSender?.TellTraced(response);
        });
        CommandAny(msg => Stash.Stash());
    }
}