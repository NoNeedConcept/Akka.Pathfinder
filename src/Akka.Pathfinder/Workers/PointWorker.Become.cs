using Akka.Pathfinder.Core.Messages;
using Akka.Persistence;
using Akka.Actor;
using Akka.Cluster.Sharding;

namespace Akka.Pathfinder.Workers;

public partial class PointWorker
{
    private void Initialize()
    {
        _logger.Verbose("[{PointId}][INITIALIZE]", EntityId);
        Command<InitializePoint>(InitializePointHandler);
        Command<ReceiveTimeout>(msg => Context.Parent.Tell(new Passivate(PoisonPill.Instance)));
        CommandAny(msg => Stash.Stash());
    }

    private void Configure()
    {
        _logger.Verbose("[{PointId}][CONFIGURE]", EntityId);
        Command<LocalPointConfig>(LocalPointConfigHandler);
        CommandAny(msg => Stash.Stash());
        OnConfigure();
    }

    private void Update()
    {
        _logger.Verbose("[{PointId}][UPDATE]", EntityId);
        Command<LocalPointConfig>(LocalPointConfigHandler);
        CommandAny(msg => Stash.Stash());
    }

    private void Failure()
    {
        _logger.Verbose("[{PointId}][FAILURE]", EntityId);
        var deleteSender = ActorRefs.NoSender;
        DeletePointRequest request = null!;
        Command<ReceiveTimeout>(msg => Context.Parent.Tell(new Passivate(PoisonPill.Instance)));
        Command<DeletePointRequest>(msg =>
        {
            deleteSender = Sender;
            request = msg;
            DeleteSnapshots(new SnapshotSelectionCriteria(SnapshotSequenceNr, DateTime.Now));
        });
        Command<DeleteSnapshotsSuccess>(msg =>
        {
            var response = new DeletePointResponse(request.RequestId, request.PointId, true);
            deleteSender.Tell(response);
        });
        Command<DeleteSnapshotFailure>(msg =>
        {
            var response = new DeletePointResponse(request.RequestId, request.PointId, false, msg.Cause);
            deleteSender.Tell(response);
        });
        CommandAny(msg =>
        {
            _logger.Debug("[{PointId}][{MessageType}] message received -> no action in failure state", EntityId, msg.GetType().Name);
        });
    }

    private void Ready()
    {
        _logger.Verbose("[{PointId}][READY]", EntityId);
        // Sender -> PathfinderWorker
        Command<FindPathRequest>(FindPathRequestHandler);
        Command<PathfinderDeactivated>(PathfinderDeactivatedHandler);
        // Sender -> MapManager 
        Command<CostRequest>(CostRequestHandler);
        Command<PointCommandRequest>(PointCommandRequestHandler);
        Command<InitializePoint>(msg => Sender.Tell(new PointInitialized(msg.RequestId, msg.PointId)));
        Command<UpdatePointDirection>(UpdatePointDirectionHandler);
        Command<ReloadPoint>(ReloadPointHandler);
        // Sender -> SnapshotStore
        Command<SaveSnapshotSuccess>(SaveSnapshotSuccessHandler);
        Command<SaveSnapshotFailure>(SaveSnapshotFailureHandler);
        Command<ReceiveTimeout>(msg => Context.Parent.Tell(new Passivate(PoisonPill.Instance)));
        Stash.UnstashAll();
    }
}
