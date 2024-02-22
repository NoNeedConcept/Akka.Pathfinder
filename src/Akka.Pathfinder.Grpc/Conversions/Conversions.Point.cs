using Akka.Pathfinder.Core.Messages;

namespace Akka.Pathfinder.Grpc.Conversions;

public static partial class Conversions
{
    public static OccupiedPoint ToOccupied(this PointRequest request)
        => new(request.PointId);

    public static ReleasedPoint ToReleased(this PointRequest request)
        => new(request.PointId);

    public static BlockPointCommandRequest ToBlock(this PointRequest request)
        => new(request.PointId);

    public static UnblockPointCommandRequest ToUnblock(this PointRequest request)
        => new(request.PointId);

    public static UpdatePointDirection ToUpdateDirection(this PointConfig request)
        => new(request.To());

    public static IncreasePointCostRequest ToIncrease(this UpdateCostRequest request)
        => new(request.PointId, request.Value);

    public static DecreasePointCostRequest ToDecrease(this UpdateCostRequest request) =>
        new(request.PointId, request.Value);

    public static IncreaseDirectionCostRequest ToIncrease(this UpdateDirectionCostRequest request)
        => new(request.PointId, request.Value, request.Direction.To());

    public static DecreaseDirectionCostRequest ToDecrease(this UpdateDirectionCostRequest request)
        => new(request.PointId, request.Value, request.Direction.To());
}