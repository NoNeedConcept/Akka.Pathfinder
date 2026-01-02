using Akka.Actor;
using Akka.Hosting;
using Akka.Pathfinder.Core.Messages;
using Servus.Akka.Diagnostics;

namespace Akka.Pathfinder;

internal class PointGatewayService : IPointGatewayService
{
    private readonly IActorRef _pointClient;

    public PointGatewayService(IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.CreateScope();
        var actorRegistry = scope.ServiceProvider.GetRequiredService<IReadOnlyActorRegistry>();
        _pointClient = actorRegistry.Get<RequestForwarder>();
    }

    public Task<UpdateCostResponse> OccupyAsync(OccupiedPoint request, CancellationToken cancellationToken = default)
        => _pointClient.AskTraced<UpdateCostResponse>(request);

    public Task<UpdateCostResponse> ReleaseAsync(ReleasedPoint request, CancellationToken cancellationToken = default)
        => _pointClient.AskTraced<UpdateCostResponse>(request);

    public Task<PointCommandResponse> BlockAsync(BlockPointCommandRequest request,
        CancellationToken cancellationToken = default)
        => _pointClient.AskTraced<PointCommandResponse>(request);

    public Task<PointCommandResponse> UnblockAsync(UnblockPointCommandRequest request,
        CancellationToken cancellationToken = default)
        => _pointClient.AskTraced<PointCommandResponse>(request);

    public Task<PointInitialized> InitializeAsync(InitializePoint request,
        CancellationToken cancellationToken = default)
        => _pointClient.AskTraced<PointInitialized>(request);

    public Task<PointDirectionUpdated> UpdateDirectionAsync(UpdatePointDirection request,
        CancellationToken cancellationToken = default)
        => _pointClient.AskTraced<PointDirectionUpdated>(request);

    public Task<PointDeleted> DeleteAsync(DeletePoint request, CancellationToken cancellationToken = default)
        => _pointClient.AskTraced<PointDeleted>(request);

    public Task<UpdateCostResponse> IncreaseCostAsync(IncreasePointCostRequest request,
        CancellationToken cancellationToken = default)
        => _pointClient.AskTraced<UpdateCostResponse>(request);

    public Task<UpdateCostResponse> DecreaseCostAsync(DecreasePointCostRequest request,
        CancellationToken cancellationToken = default)
        => _pointClient.AskTraced<UpdateCostResponse>(request);

    public Task<UpdateCostResponse> IncreaseDirectionCostAsync(IncreaseDirectionCostRequest request,
        CancellationToken cancellationToken = default)
        => _pointClient.AskTraced<UpdateCostResponse>(request);

    public Task<UpdateCostResponse> DecreaseDirectionCostAsync(DecreaseDirectionCostRequest request,
        CancellationToken cancellationToken = default)
        => _pointClient.AskTraced<UpdateCostResponse>(request);
}