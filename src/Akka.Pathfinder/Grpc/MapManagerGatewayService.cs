using Akka.Actor;
using Akka.Hosting;
using Akka.Pathfinder.Core.Messages;

namespace Akka.Pathfinder;

internal class MapManagerGatewayService : IMapManagerGatewayService
{
    private readonly IActorRef _mapManagerClient;
    public MapManagerGatewayService(IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.CreateScope();
        var actorRegistry = scope.ServiceProvider.GetRequiredService<IReadOnlyActorRegistry>();
        _mapManagerClient = actorRegistry.Get<RequestForwarder>();
    }

    public async Task<MapLoaded> LoadAsync(LoadMap request, CancellationToken cancellationToken = default)
        => await _mapManagerClient.Ask<MapLoaded>(request, cancellationToken);

    public async Task<MapUpdated> UpdateAsync(UpdateMap request, CancellationToken cancellationToken = default)
        => await _mapManagerClient.Ask<MapUpdated>(request, cancellationToken);
}