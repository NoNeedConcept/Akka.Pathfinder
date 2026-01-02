using Akka.Pathfinder.Core.Messages;
using Akka.Cluster.Sharding;
using Akka.Actor;
using Akka.Persistence;

namespace Akka.Pathfinder.Workers;

public partial class PathfinderWorker
{
    private void Ready()
    {
        _logger.Information("[{PathfinderId}][READY]", _entityId);
        Command<PathfinderRequest>(PathfinderRequestHandler);
        Command<PathFound>(FoundPathHandler);
        // Sender -> Self
        Command<Timeout>(TimeoutHandler);
        // Sender -> SnapshotStore
        Command<SaveSnapshotSuccess>(SaveSnapshotSuccessHandler);
        Command<SaveSnapshotFailure>(SaveSnapshotFailureHandler);
        Command<ReceiveTimeout>(msg => Context.Parent.Tell(new Passivate(PoisonPill.Instance)));
        Stash.UnstashAll();
    }

    private void Void()
    {
        _logger.Debug("[{PathfinderId}][VOID]", _entityId);
        Command<ReceiveTimeout>(msg => Context.Parent.Tell(new Passivate(PoisonPill.Instance)));
        CommandAny(msg => _logger.Debug("[{PathfinderId}][{MessageType}] message received -> VOID", _entityId, msg.GetType().Name));
    }

    private void Failure()
    {
        _logger.Warning("[{PathfinderId}][FAILURE]", _entityId);
        var deleteSender = ActorRefs.NoSender;
        DeletePathfinder request = null!;
        Command<ReceiveTimeout>(msg => Context.Parent.Tell(new Passivate(PoisonPill.Instance)));
        Command<DeletePathfinder>(msg =>
        {
            deleteSender = Sender;
            request = msg;
            DeleteSnapshots(new SnapshotSelectionCriteria(SnapshotSequenceNr, DateTime.Now));
        });
        Command<DeleteSnapshotsSuccess>(msg =>
        {
            var response = new PathfinderDeleted(request.RequestId, request.PathfinderId, true);
            deleteSender.Tell(response);
        });
        Command<DeleteSnapshotFailure>(msg =>
        {
            var response = new PathfinderDeleted(request.RequestId, request.PathfinderId, false, msg.Cause);
            deleteSender.Tell(response);
        });
        CommandAny(msg =>
        {
            _logger.Debug("[{PathfinderId}][{MessageType}] message received -> no action in failure state", _entityId, msg.GetType().Name);
        });
    }
}
