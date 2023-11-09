using Akka.Pathfinder.Core.Messages;
using Akka.Cluster.Sharding;
using Akka.Actor;

namespace Akka.Pathfinder.Workers;

public partial class PathfinderWorker
{
    private void WhilePathEvaluation()
    {
        Command<BestPathFound>(BestPathFoundHandler);
        Command<BestPathFailed>(BestPathFailedHandler);
        CommandAny(msg => Stash.Stash());
    }

    private void Void()
    {
        Command<ReceiveTimeout>(msg => Context.Parent.Tell(new Passivate(PoisonPill.Instance)));
        CommandAny(msg => _logger.Debug("[{PathfinderId}][{MessageType}] message received -> VOID", EntityId, msg.GetType().Name));
    }
}
