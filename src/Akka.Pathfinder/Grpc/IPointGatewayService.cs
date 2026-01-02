using Akka.Pathfinder.Core.Messages;

namespace Akka.Pathfinder;

internal interface IPointGatewayService
{
    Task<UpdateCostResponse> OccupyAsync(OccupiedPoint request, CancellationToken cancellationToken = default);
    Task<UpdateCostResponse> ReleaseAsync(ReleasedPoint request, CancellationToken cancellationToken = default);
    Task<PointCommandResponse> BlockAsync(BlockPointCommandRequest request, CancellationToken cancellationToken = default);
    Task<PointCommandResponse> UnblockAsync(UnblockPointCommandRequest request, CancellationToken cancellationToken = default);
    Task<PointDirectionUpdated> UpdateDirectionAsync(UpdatePointDirection request, CancellationToken cancellationToken = default);
    Task<UpdateCostResponse> IncreaseCostAsync(IncreasePointCostRequest request, CancellationToken cancellationToken = default);
    Task<UpdateCostResponse> DecreaseCostAsync(DecreasePointCostRequest request, CancellationToken cancellationToken = default);
    Task<UpdateCostResponse> IncreaseDirectionCostAsync(IncreaseDirectionCostRequest request, CancellationToken cancellationToken = default);
    Task<UpdateCostResponse> DecreaseDirectionCostAsync(DecreaseDirectionCostRequest request, CancellationToken cancellationToken = default);
}
