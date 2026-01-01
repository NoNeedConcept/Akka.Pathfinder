using Servus.Core.Diagnostics;

namespace Akka.Pathfinder.Core.Messages;


public record GetMapState(Guid MapId) : MapRequestBase<MapStateResponse>;
public record MapStateResponse(Guid RequestId, Guid MapId, bool IsReady) : ResponseBase(RequestId); 
public record LoadMap(Guid MapId) : MapRequestBase<MapLoaded>;
public record MapLoaded(Guid RequestId, Guid MapId) : ResponseBase(RequestId);
public record UpdateMap(Guid MapId) : MapRequestBase<MapUpdated>;
public record MapUpdated(Guid RequestId, Guid MapId) : ResponseBase(RequestId);

public abstract record MapRequestBase<TRequest>() : RequestBase<TRequest>(Guid.NewGuid()), IMapManagerRequest<TRequest>, IWithTracing where TRequest : IResponse
{
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
}