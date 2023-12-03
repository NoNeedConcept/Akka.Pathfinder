using Akka.Pathfinder.Core.Configs;

namespace Akka.Pathfinder.Core.Messages;

public record PathfinderStartRequest(Guid PathfinderId, int SourcePointId, int TargetPointId, Direction Direction, TimeSpan? Timeout = default) : PathfinderRequest(PathfinderId);
public record FindPathRequestStarted(Guid PathfinderId) : PathfinderRequest(PathfinderId);
public record PathFound(Guid PathfinderId, Guid PathId, PathfinderResult Result) : PathfinderRequest(PathfinderId);
public record BestPathFound(Guid PathfinderId, Guid PathId) : PathfinderRequest(PathfinderId);
public record BestPathFailed(Guid PathfinderId, Exception Exception) : PathfinderRequest(PathfinderId);
public record PathfinderTimeout(Guid PathfinderId) : IPathfinderId;
public abstract record PathfinderRequest(Guid PathfinderId) : IPathfinderId;

public enum PathfinderResult : byte
{
    Unknown,
    Success,
    PathBlocked,
    MindBlown
}