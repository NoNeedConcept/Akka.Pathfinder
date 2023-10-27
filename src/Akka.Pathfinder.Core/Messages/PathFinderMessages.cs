using Akka.Pathfinder.Core.Configs;
using Path = Akka.Pathfinder.Core.Persistence.Data.Path;

namespace Akka.Pathfinder.Core.Messages;

public abstract record PointRequest(int PointId) : IPointId;
public record FindPathRequest(Guid PathfinderId, Guid PathId, int NextPointId, int TargetPointId, IReadOnlyList<PathPoint> Directions) : PointRequest(NextPointId);
public record PathfinderStartRequest(Guid PathfinderId, int SourcePointId, int TargetPointId, Direction Direction, TimeSpan? Timeout = default) : PathFinderRequest(PathfinderId);
public record PathFound(Guid PathfinderId, Guid PathId, PathFinderResult Result) : PathFinderRequest(PathfinderId);
public record BestPathFound(Guid PathfinderId, Guid PathId) : PathFinderRequest(PathfinderId);
public record BestPathFailed(Guid PathfinderId, Exception Exception) : PathFinderRequest(PathfinderId);
public record PathPoint(int PointId, uint Cost, Direction Direction);
public record FickDichPatrick(Guid PathfinderId) : IPathfinderId;

public record PathfinderDeactivated(Guid PathfinderId);

public record PathFinderDone(Path? Path);
public abstract record PathFinderRequest(Guid PathfinderId) : IPathfinderId;

public enum PathFinderResult : byte
{
    Unknown,
    Success,
    PathBlocked,
    LoopDetected,
    MindBlown
}
