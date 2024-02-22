using Akka.Pathfinder.Core.Messages;
using Akka.Actor;
using Akka.Persistence;
using Servus.Akka.Diagnostics;

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
        Stash.UnstashAll();
    }

    private void Void()
    {
        _logger.Information("[{PathfinderId}][VOID]", _entityId);
        CommandAny(msg => _logger.Debug("[{PathfinderId}][{MessageType}] message received -> VOID", _entityId, msg.GetType().Name));
    }

    private void Failure()
    {
        _logger.Warning("[{PathfinderId}][FAILURE]", _entityId);
        var deleteSender = ActorRefs.NoSender;
        DeletePathfinder request = null!;
        Command<DeletePathfinder>(msg =>
        {
            deleteSender = Sender;
            request = msg;
            DeleteSnapshots(new SnapshotSelectionCriteria(SnapshotSequenceNr, DateTime.Now));
        });
        Command<DeleteSnapshotsSuccess>(msg =>
        {
            var response = new PathfinderDeleted(request.RequestId, request.PathfinderId, true);
            deleteSender?.TellTraced(response);
        });
        Command<DeleteSnapshotFailure>(msg =>
        {
            var response = new PathfinderDeleted(request.RequestId, request.PathfinderId, false, msg.Cause);
            deleteSender?.TellTraced(response);
        });
    }
}
