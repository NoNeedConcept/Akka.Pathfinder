using Akka.Pathfinder.Core.Messages;

namespace Akka.Pathfinder.Core;

public class PathfinderWorkerState
{
    public static PathfinderWorkerState FromRequest(PathfinderStartRequest msg)
        => new()
        {
            PathfinderId = msg.PathfinderId,
            TargetPointId = msg.TargetPointId,
            SourcePointId = msg.SourcePointId
        };

    private int _counter;

    internal PathfinderWorkerState() => _counter = 0;

    public Guid PathfinderId { get; init; }
    public int SourcePointId { get; init; }
    public int TargetPointId { get; init; }

    public int Count => _counter;

    public bool HasPathFound => _counter != 0;

    public void IncrementFoundPathCounter() => ++_counter;
}