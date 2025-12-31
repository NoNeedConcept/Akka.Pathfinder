using Servus.Core.Diagnostics;

namespace Akka.Pathfinder.Core.Messages;

public record SavePathfinderSender(Guid PathfinderId);
public record ForwardToPathfinderSender(Guid PathfinderId, IResponse Message) : IWithTracing
{
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
}