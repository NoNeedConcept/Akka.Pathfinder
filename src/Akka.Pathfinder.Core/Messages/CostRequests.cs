using Akka.Pathfinder.Core.Configs;

namespace Akka.Pathfinder.Core.Messages;

public static class CostConstants
{
    public const uint OccupiedCost = 420;
}

public record UpdateCostResponse(int PointId, bool Success);

public abstract record PointRequest(int PointId) : IPointId;

public abstract record CostRequest(int PointId, uint Value, ChangeMethod ChangeMethod) : PointRequest(PointId);

public abstract record PointCostRequest(int PointId, uint Value, ChangeMethod ChangeMethod) : CostRequest(PointId, Value, ChangeMethod);

public abstract record IncreasePointCostRequest(int PointId, uint Value) : PointCostRequest(PointId, Value, ChangeMethod.Increase);
public abstract record DecreasePointCostRequest(int PointId, uint Value) : PointCostRequest(PointId, Value, ChangeMethod.Decrease);

public record OccupiedPoint(int PointId) : IncreasePointCostRequest(PointId, CostConstants.OccupiedCost);
public record ReleasedPoint(int PointId) : DecreasePointCostRequest(PointId, CostConstants.OccupiedCost);

public abstract record DirectionCostRequest(int PointId, uint Value, Direction Direction, ChangeMethod ChangeMethod) : CostRequest(PointId, Value, ChangeMethod);

public abstract record IncreaseDirectionCostRequest(int PointId, uint Value, Direction Direction) : DirectionCostRequest(PointId, Value, Direction, ChangeMethod.Increase);
public abstract record DecreaseDirectionCostRequest(int PointId, uint Value, Direction Direction) : DirectionCostRequest(PointId, Value, Direction, ChangeMethod.Decrease);

public abstract record PointCommandRequest(int PointId) : IPointId;

public record BlockPointCommandRequest(int PointId) : PointCommandRequest(PointId);
public record UnblockPointCommandRequest(int PointId) : PointCommandRequest(PointId);
public record InitializePoint(int PointId, Guid CollectionId): PointRequest(PointId);
public record UpdatePointDirection(PointConfig Config) : PointRequest(Config.Id);
public record ResetPoint(PointConfig Config) : PointRequest(Config.Id);
public record FindPathRequest(Guid PathfinderId, Guid PathId, int NextPointId, int TargetPointId, IReadOnlyList<PathPoint> Directions) : PointRequest(NextPointId);