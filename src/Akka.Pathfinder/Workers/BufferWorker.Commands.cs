using Akka.Pathfinder.Core;

namespace Akka.Pathfinder;

public partial class BufferWorker
{
    private void InitializeBufferHandler(InitializeBuffer msg)
    {
        _logger.Debug("[{BufferId}][{MessageType}] received", EntityId, msg.GetType().Name);
        int maxPoints = _mapConfigReader.Get(msg.MapId).Count;
        _state = new(maxPoints);
        Become(Ready);
    }

    private void PathfinderHasPointsArrivedHandler(PathfinderHasPointsArrived msg)
    {
        _logger.Debug("[{BufferId}][{MessageType}] received", EntityId, msg.GetType().Name);
        
    }
}
