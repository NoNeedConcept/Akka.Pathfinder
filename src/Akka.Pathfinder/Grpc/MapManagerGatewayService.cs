using Akka.Actor;
using Akka.Hosting;
using Akka.Pathfinder.Core.Messages;
using Servus.Akka.Diagnostics;

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

    public Task<MapStateResponse> GetMapState(GetMapState request, CancellationToken cancellationToken)
        => _mapManagerClient.AskTraced<MapStateResponse>(request);

    public Task<MapLoaded> LoadAsync(LoadMap request, CancellationToken cancellationToken = default)
        => _mapManagerClient.AskTraced<MapLoaded>(request);

    public Task<MapUpdated> UpdateAsync(UpdateMap request, CancellationToken cancellationToken = default)
        => _mapManagerClient.AskTraced<MapUpdated>(request);
}