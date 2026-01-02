using Akka.Actor;
using Akka.Hosting;
using Akka.Pathfinder.Core.Messages;
using Servus.Akka.Diagnostics;

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

    public Task<PathfinderResponse> FindPathAsync(PathfinderRequest request, CancellationToken cancellationToken = default)
        => _pathfinderClient.AskTraced<PathfinderResponse>(request);

    public Task<PathfinderDeleted> DeleteAsync(DeletePathfinder request, CancellationToken cancellationToken = default)
        => _pathfinderClient.AskTraced<PathfinderDeleted>(request);
}