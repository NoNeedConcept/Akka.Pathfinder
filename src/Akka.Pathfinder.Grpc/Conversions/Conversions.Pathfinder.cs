using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.Persistence.Data;
using Directions = Akka.Pathfinder.Core.Configs.Directions;

namespace Akka.Pathfinder.Grpc.Conversions;

public static partial class Conversions
{
    public static PathfinderRequest To(this Grpc.FindPathRequest value)
        => new(value.PathfinderId.To(), value.SourcePointId, value.TargetPointId, value.Direction.To(), new PathfinderOptions(AlgoMode.Timeout, value.Duration.ToTimeSpan()));

    public static DeletePathfinder To(this DeletePathfinderRequest value)
        => new(Guid.NewGuid(), value.PathfinderId.To());

    public static DeletePathfinderResponse To(this PathfinderDeleted value)
        => new() { Success = value.Success, ErrorMessage = value.Error?.Message ?? string.Empty };

    public static FindPathResponse To(this PathfinderResponse value, int pathCost = 0)
        => new()
        {
            PathCost = pathCost,
            PathfinderId = value.PathfinderId.ToString(),
            PathId = value.PathId.ToString(),
            Success = value.Success,
            ErrorMessage = string.Empty
        };

    public static Point To(this PathPoint value)
        => new() { Id = value.PointId, Cost = value.Cost, Direction = value.Direction.To() };

    public static Guid To(this string value)
        => Guid.Parse(value);

    public static Directions To(this Direction value)
        => value switch
        {
            Direction.None => Directions.None,
            Direction.Top => Directions.Top,
            Direction.Bottom => Directions.Bottom,
            Direction.Left => Directions.Left,
            Direction.Right => Directions.Right,
            Direction.Front => Directions.Front,
            Direction.Back => Directions.Back,
            _ => Directions.None
        };

    public static Direction To(this Directions value)
        => value switch
        {
            Directions.None => Direction.None,
            Directions.Top => Direction.Top,
            Directions.Bottom => Direction.Bottom,
            Directions.Left => Direction.Left,
            Directions.Right => Direction.Right,
            Directions.Front => Direction.Front,
            Directions.Back => Direction.Back,
            _ => Direction.None
        };
}
