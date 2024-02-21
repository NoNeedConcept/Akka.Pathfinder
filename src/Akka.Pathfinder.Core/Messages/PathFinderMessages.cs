using Akka.Pathfinder.Core.Configs;

namespace Akka.Pathfinder.Core.Messages;

public record PathFound(Guid RequestId, Guid PathfinderId, Guid PathId, PathfinderResult Result) : ResponseBase(RequestId), IPathfinderId;

public record PathfinderRequest(Guid PathfinderId, int SourcePointId, int TargetPointId, Direction Direction, TimeSpan? Timeout = default) : PathfinderRequestBase<PathfinderResponse>(PathfinderId);

public record PathfinderResponse(Guid RequestId, Guid PathfinderId, bool Success, Guid? PathId = default, string? ErrorMessage = default) : ResponseBase(RequestId);

public record DeletePathfinderRequest(Guid RequestId, Guid PathfinderId) : RequestBase<DeletePathfinderResponse>(RequestId), IPathfinderId;
public record DeletePathfinderResponse(Guid RequestId, Guid PathfinderId, bool Success = false, Exception? Error = default) : ResponseBase(RequestId);

public abstract record PathfinderRequestBase<TResponse>(Guid PathfinderId) : RequestBase<TResponse>(Guid.NewGuid()), IPathfinderRequest<TResponse> where TResponse : IResponse;

public enum PathfinderResult : byte
{
    Unknown,
    Success,
    PathBlocked
}

// over EventStream
public record PathfinderDeactivated(Guid PathfinderId);