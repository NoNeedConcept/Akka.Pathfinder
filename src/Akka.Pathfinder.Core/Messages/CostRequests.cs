using Akka.Pathfinder.Core.Configs;
using Akka.Pathfinder.Core.Persistence.Data;

namespace Akka.Pathfinder.Core.Messages;

public static class CostConstants
{
    public const uint OccupiedCost = 420;
}

public record UpdateCostResponse(Guid RequestId, int PointId, bool Success): ResponseBase(RequestId);

public abstract record PointRequest<TResponse>(int PointId) : RequestBase<TResponse>(Guid.NewGuid()), IPointId where TResponse : IResponse;

public abstract record CostRequest(int PointId, uint Value, ChangeMethod ChangeMethod) : PointRequest<UpdateCostResponse>(PointId);

public abstract record PointCostRequest(int PointId, uint Value, ChangeMethod ChangeMethod) : CostRequest(PointId, Value, ChangeMethod);

public abstract record IncreasePointCostRequest(int PointId, uint Value) : PointCostRequest(PointId, Value, ChangeMethod.Increase);
public abstract record DecreasePointCostRequest(int PointId, uint Value) : PointCostRequest(PointId, Value, ChangeMethod.Decrease);

public record OccupiedPoint(int PointId) : IncreasePointCostRequest(PointId, CostConstants.OccupiedCost);
public record ReleasedPoint(int PointId) : DecreasePointCostRequest(PointId, CostConstants.OccupiedCost);

public abstract record DirectionCostRequest(int PointId, uint Value, Direction Direction, ChangeMethod ChangeMethod) : CostRequest(PointId, Value, ChangeMethod);

public abstract record IncreaseDirectionCostRequest(int PointId, uint Value, Direction Direction) : DirectionCostRequest(PointId, Value, Direction, ChangeMethod.Increase);
public abstract record DecreaseDirectionCostRequest(int PointId, uint Value, Direction Direction) : DirectionCostRequest(PointId, Value, Direction, ChangeMethod.Decrease);

public abstract record PointCommandRequest(int PointId) : PointRequest<PointCommandResponse>(PointId);
public record PointCommandResponse(Guid RequestId, int PointId) : ResponseBase(RequestId);

public record BlockPointCommandRequest(int PointId) : PointCommandRequest(PointId);
public record UnblockPointCommandRequest(int PointId) : PointCommandRequest(PointId);
public record InitializePoint(int PointId, Guid CollectionId) : PointRequest<PointInitialized>(PointId);
public record PointInitialized(Guid RequestId, int PointId) : ResponseBase(RequestId);
public record UpdatePointDirection(PointConfig Config) : PointRequest<PointDirectionUpdated>(Config.Id);
public record PointDirectionUpdated(Guid RequestId, int PointId) : ResponseBase(RequestId);
public record ReloadPoint(int PointId, Guid CollectionId) : PointRequest<PointReloaded>(PointId);
public record PointReloaded(Guid RequestId, int PointId) : ResponseBase(RequestId);
public record FindPathRequest(Guid PathfinderId, Guid PathId, int NextPointId, int TargetPointId, IReadOnlyList<PathPoint> Directions) : PointRequest<PathFound>(NextPointId);

public record DeletePointRequest(int PointId) : PointRequest<DeletePointResponse>(PointId);
public record DeletePointResponse(Guid RequestId, int PointId, bool Success = false, Exception Error = default!): ResponseBase(RequestId);