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
        // Sender -> Requester
        Command<PathfinderStartRequest>(FindPathHandler);
        // Sender -> MapManager
        Command<FindPathRequestStarted>(FindPathRequestStarted);
        Command<PathFound>(FoundPathHandler);
        // Sender -> Self
        CommandAsync<PathfinderTimeout>(PathfinderTimeoutHandler);
        // Sender -> SnapshotStore
        Command<SaveSnapshotSuccess>(SaveSnapshotSuccessHandler);
        Command<SaveSnapshotFailure>(SaveSnapshotFailureHandler);
        Command<ReceiveTimeout>(msg => Context.Parent.Tell(new Passivate(PoisonPill.Instance)));
        Stash.UnstashAll();
    }

    private void WhilePathEvaluation()
    {
        _logger.Information("[{PathfinderId}][WhilePathEvaluation]", EntityId);
        Command<BestPathFound>(BestPathFoundHandler);
        Command<BestPathFailed>(BestPathFailedHandler);
        CommandAny(msg => Stash.Stash());
    }

    private void Void()
    {
        _logger.Information("[{PathfinderId}][VOID]", EntityId);
        Command<ReceiveTimeout>(msg => Context.Parent.Tell(new Passivate(PoisonPill.Instance)));
        CommandAny(msg => _logger.Debug("[{PathfinderId}][{MessageType}] message received -> VOID", EntityId, msg.GetType().Name));
    }

    private void Failure()
    {
        _logger.Information("[{PathfinderId}][FAILURE]", EntityId);
        Command<ReceiveTimeout>(msg => Context.Parent.Tell(new Passivate(PoisonPill.Instance)));
        CommandAny(msg =>
        {
            _logger.Debug("[{PathfinderId}][{MessageType}] message received -> no action in failure state", EntityId, msg.GetType().Name);
        });
    }
}
