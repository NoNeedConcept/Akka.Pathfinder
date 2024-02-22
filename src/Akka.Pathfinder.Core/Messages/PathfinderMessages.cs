using Akka.Pathfinder.Core.Configs;

namespace Akka.Pathfinder.Core.Messages;

public record PathFound(Guid RequestId, Guid PathfinderId, Guid PathId, PathfinderResult Result) : ResponseBase(RequestId), IPathfinderId;

public record PathfinderRequest(Guid PathfinderId, int SourcePointId, int TargetPointId, Directions Direction, PathfinderOptions Options) : PathfinderRequestBase<PathfinderResponse>(PathfinderId);
public record PathfinderOptions(AlgoMode Mode = AlgoMode.Timeout, TimeSpan? Timeout = null);
public record PathfinderResponse(Guid RequestId, Guid PathfinderId, bool Success, Guid? PathId = null, string? ErrorMessage = null) : ResponseBase(RequestId);

public record DeletePathfinder(Guid RequestId, Guid PathfinderId) : RequestBase<PathfinderDeleted>(RequestId), IPathfinderId;
public record PathfinderDeleted(Guid RequestId, Guid PathfinderId, bool Success = false, Exception? Error = null) : ResponseBase(RequestId);

public abstract record PathfinderRequestBase<TResponse>(Guid PathfinderId) : RequestBase<TResponse>(Guid.NewGuid()), IPathfinderRequest<TResponse> where TResponse : IResponse;

public enum AlgoMode : byte
{
    First,
    Timeout
}

public enum PathfinderResult : byte
{
    Unknown,
    Success,
    PathBlocked
}

// over EventStream
public record PathfinderDeactivated(Guid PathfinderId) : MessageBase;