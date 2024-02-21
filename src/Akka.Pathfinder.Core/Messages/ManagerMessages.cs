namespace Akka.Pathfinder.Core.Messages;

public record LoadMap(Guid MapId) : MapRequestBase<MapLoaded>();
public record MapLoaded(Guid RequestId, Guid MapId) : ResponseBase(RequestId);
public record UpdateMap(Guid MapId) : MapRequestBase<MapUpdated>();
public record MapUpdated(Guid RequestId, Guid MapId) : ResponseBase(RequestId);

public abstract record MapRequestBase<TRequest>() : RequestBase<TRequest>(Guid.NewGuid()), IMapManagerRequest<TRequest> where TRequest : IResponse;