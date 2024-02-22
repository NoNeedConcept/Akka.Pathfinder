using moin.akka.endpoint;

namespace Akka.Pathfinder.Core;

[RoleDefinition("Pathfinder")]
public record Endpoint
{
    [EndpointDefinition("MapManager", EndpointType.Singleton)]
    public record MapManager : IEndpointDefinition;
    [EndpointDefinition("SenderManager", EndpointType.Singleton)]
    public record SenderManager : IEndpointDefinition;
    [EndpointDefinition("Pathfinder", EndpointType.Shard)]
    public record PathfinderWorker : IEndpointDefinition;
    [EndpointDefinition("Point", EndpointType.Shard)]
    public record PointWorker : IEndpointDefinition;
}