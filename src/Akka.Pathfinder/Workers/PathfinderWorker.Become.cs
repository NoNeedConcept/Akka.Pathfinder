using Akka.Pathfinder.Core.Messages;
using Akka.Cluster.Sharding;
using Akka.Actor;
using Akka.Persistence;

namespace Akka.Pathfinder.Workers;

public partial class PathfinderWorker
{
    private void Ready()
    {
        _logger.Information("[{PathfinderId}][READY]", EntityId);
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
        _logger.Debug("[{PathfinderId}][VOID]", EntityId);
        Command<ReceiveTimeout>(msg => Context.Parent.Tell(new Passivate(PoisonPill.Instance)));
        CommandAny(msg => _logger.Debug("[{PathfinderId}][{MessageType}] message received -> VOID", EntityId, msg.GetType().Name));
    }

    private void Failure()
    {
        _logger.Warning("[{PathfinderId}][FAILURE]", EntityId);
        var deleteSender = ActorRefs.NoSender;
        DeletePathfinderRequest request = null!;
        Command<ReceiveTimeout>(msg => Context.Parent.Tell(new Passivate(PoisonPill.Instance)));
        Command<DeletePathfinderRequest>(msg =>
        {
            deleteSender = Sender;
            request = msg;
            DeleteSnapshots(new SnapshotSelectionCriteria(SnapshotSequenceNr, DateTime.Now));
        });
        Command<DeleteSnapshotsSuccess>(msg =>
        {
            var response = new DeletePathfinderResponse(request.RequestId, request.PathfinderId, true);
            deleteSender.Tell(response);
        });
        Command<DeleteSnapshotFailure>(msg =>
        {
            var response = new DeletePathfinderResponse(request.RequestId, request.PathfinderId, false, msg.Cause);
            deleteSender.Tell(response);
        });
        CommandAny(msg =>
        {
            _logger.Debug("[{PathfinderId}][{MessageType}] message received -> no action in failure state", EntityId, msg.GetType().Name);
        });
    }
}
