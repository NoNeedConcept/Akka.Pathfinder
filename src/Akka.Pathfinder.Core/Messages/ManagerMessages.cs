namespace Akka.Pathfinder.Core.Messages;

public record GetMapState(Guid MapId) : MapRequestBase<MapStateResponse>;

public record MapStateResponse(Guid RequestId, Guid MapId, bool IsReady) : ResponseBase(RequestId);

public record LoadMap(Guid MapId) : MapRequestBase<MapLoaded>;

public record MapLoaded(Guid RequestId, Guid MapId, bool Success = true) : ResponseBase(RequestId);

public record DeleteMap(Guid MapId) : MapRequestBase<MapDeleted>;

public record MapDeleted(Guid RequestId, Guid MapId, bool Success = false, string? ErrorMessage = null)
    : ResponseBase(RequestId);

public abstract record MapRequestBase<TRequest>()
    : RequestBase<TRequest>(Guid.NewGuid()), IMapManagerRequest<TRequest> where TRequest : IResponse;