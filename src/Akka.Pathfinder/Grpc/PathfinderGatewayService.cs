using Akka.Actor;
using Akka.Hosting;
using Akka.Pathfinder.Core.Messages;

namespace Akka.Pathfinder;

internal class PathfinderGatewayService : IPathfinderGatewayService
{
    private readonly IActorRef _pathfinderClient;
    public PathfinderGatewayService(IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.CreateScope();
        var actorRegistry = scope.ServiceProvider.GetRequiredService<IReadOnlyActorRegistry>();
        _pathfinderClient = actorRegistry.Get<RequestForwarder>();
    }

    public async Task<PathfinderResponse> FindPathAsync(PathfinderRequest request, CancellationToken cancellationToken = default)
        => await _pathfinderClient.Ask<PathfinderResponse>(request, cancellationToken);
}