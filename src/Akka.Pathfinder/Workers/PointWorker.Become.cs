using Akka.Pathfinder.Core.Messages;
using Akka.Persistence;
using Akka.Actor;
using Akka.Cluster.Sharding;

namespace Akka.Pathfinder.Workers;

public partial class PointWorker
{
    private void Initialize()
    {
        _logger.Verbose("[{PointId}][INITIALIZE]", _entityId);
        Command<InitializePoint>(InitializePointHandler);
        Command<ReceiveTimeout>(msg =>
        {
            PersistState();
            Context.Parent.Tell(new Passivate(PoisonPill.Instance));
        });
        CommandAny(msg => Stash.Stash());
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
        CommandAny(msg => Stash.Stash());
    }

    private void Failure()
    {
        _logger.Verbose("[{PointId}][FAILURE]", _entityId);
        DeleteCommands();
    }

    private void Ready()
    {
        _logger.Verbose("[{PointId}][READY]", _entityId);
        // Sender -> PathfinderWorker
        Command<FindPathRequest>(FindPathRequestHandler);
        Command<PathfinderDeactivated>(PathfinderDeactivatedHandler);
        // Sender -> MapManager 
        Command<CostRequest>(CostRequestHandler);
        Command<PointCommandRequest>(PointCommandRequestHandler);
        Command<InitializePoint>(msg => Sender.Tell(new PointInitialized(msg.RequestId, msg.PointId)));
        Command<UpdatePointDirection>(UpdatePointDirectionHandler);
        // Sender -> SnapshotStore
        Command<SaveSnapshotSuccess>(SaveSnapshotSuccessHandler);
        Command<SaveSnapshotFailure>(SaveSnapshotFailureHandler);
        Command<ReceiveTimeout>(msg =>
        {
            PersistState();
            Context.Parent.Tell(new Passivate(PoisonPill.Instance));
        });

        DeleteCommands();
        Stash.UnstashAll();
    }

    private void DeleteCommands()
    {
        var deleteSender = ActorRefs.NoSender;
        DeletePoint request = null!;
        Command<ReceiveTimeout>(msg => Context.Parent.Tell(new Passivate(PoisonPill.Instance)));
        Command<DeletePoint>(msg =>
        {
            deleteSender = Sender;
            request = msg;
            DeleteSnapshots(new SnapshotSelectionCriteria(SnapshotSequenceNr, DateTime.Now));
        });
        Command<DeleteSnapshotsSuccess>(msg =>
        {
            var response = new PointDeleted(request.RequestId, request.PointId, true);
            deleteSender.Tell(response);
            Become(Initialize);
        });
        Command<DeleteSnapshotFailure>(msg =>
        {
            var response = new PointDeleted(request.RequestId, request.PointId, false, msg.Cause);
            deleteSender.Tell(response);
        });
    }
}