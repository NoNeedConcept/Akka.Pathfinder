using Akka.Pathfinder.Core.Configs;
using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.Persistence;

namespace Akka.Pathfinder.Core.States;

public class PathfinderWorkerState
{
    public static PathfinderWorkerState FromRequest(PathfinderRequest msg)
        => new()
        {
            PathfinderId = msg.PathfinderId,
            TargetPointId = msg.TargetPointId,
            SourcePointId = msg.SourcePointId,
            StartDirection = msg.Direction,
            StartTime = DateTime.UtcNow,
            Timeout = msg.Timeout ?? TimeSpan.FromSeconds(20),
        };

    public static PathfinderWorkerState FromSnapshot(PersistedPathfinderWorkerState msg)
        => new()
        {
            PathfinderId = msg.PathfinderId,
            SourcePointId = msg.SourcePointId,
            TargetPointId = msg.TargetPointId,
            StartDirection = msg.StartDirection,
            StartTime = msg.StartTime,
            Timeout = msg.Timeout,
            _counter = msg.FoundPathCounter
        };

    private int _counter;

    internal PathfinderWorkerState() => _counter = 0;

    public Guid PathfinderId { get; init; }
    public Direction StartDirection { get; init; }
    public int SourcePointId { get; init; }
    public int TargetPointId { get; init; }
    public TimeSpan Timeout { get; init; }
    public DateTime StartTime { get; init; }

    public int Count => _counter;

    public bool HasPathFound => _counter != 0;

    public void IncrementFoundPathCounter()
        => ++_counter;

    public PersistedPathfinderWorkerState GetPersistenceState()
        => new(PathfinderId, StartDirection, SourcePointId, TargetPointId, Timeout, _counter, StartTime);
}