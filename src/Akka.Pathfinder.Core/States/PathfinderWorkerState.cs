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
            Timeout = msg.Options.Timeout ?? TimeSpan.FromSeconds(30),
            Mode = msg.Options.Mode,
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
            Count = msg.FoundPathCounter,
            IsFinished = msg.IsFinished,
        };

    public Guid PathfinderId { get; private init; }
    public Directions StartDirection { get; private init; }
    public int SourcePointId { get; private init; }
    public int TargetPointId { get; private init; }
    public DateTime StartTime { get; private init; }
    public TimeSpan Timeout { get; private init; }
    public AlgoMode Mode { get; init; }
    public bool IsFinished { get; private set; }
    public int Count { get; private set; }
    public bool HasPathFound => Count != 0;
    public void IncrementFoundPathCounter() => ++Count;
    public void SetFinished() => IsFinished = true;

    public PersistedPathfinderWorkerState GetPersistenceState()
        => new(PathfinderId, StartDirection, SourcePointId, TargetPointId, Timeout, Count, StartTime);
}