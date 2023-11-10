using Akka.Pathfinder.Core;

namespace Akka.Pathfinder;

public partial class BufferWorker
{
    private void Initialize()
    {
        _logger.Debug("[{PointId}][INITIALIZE]", EntityId);
        Command<InitializeBuffer>(InitializeBufferHandler);
        CommandAny(msg => Stash.Stash());
    }
    private void Ready()
    {
        Command<PathfinderHasPointsArrived>(PathfinderHasPointsArrivedHandler);
    }
}
