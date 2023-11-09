using Akka.Pathfinder.Core.Configs;

namespace Akka.Pathfinder.Core.Messages;

public record PathfinderStartRequest(Guid PathfinderId, int SourcePointId, int TargetPointId, Direction Direction, TimeSpan? Timeout = default) : PathFinderRequest(PathfinderId);
public record PathFound(Guid PathfinderId, Guid PathId, PathFinderResult Result) : PathFinderRequest(PathfinderId);
public record BestPathFound(Guid PathfinderId, Guid PathId) : PathFinderRequest(PathfinderId);
public record BestPathFailed(Guid PathfinderId, Exception Exception) : PathFinderRequest(PathfinderId);
public record FickDichPatrick(Guid PathfinderId) : IPathfinderId;
public record MapIsReady(Guid PathfinderId) : PathFinderRequest(PathfinderId);
public abstract record PathFinderRequest(Guid PathfinderId) : IPathfinderId;

public enum PathFinderResult : byte
{
    Unknown,
    Success,
    PathBlocked,
    MindBlown
}