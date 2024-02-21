using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.Persistence.Data;
using Akka.Pathfinder.Grpc;
using Direction = Akka.Pathfinder.Core.Configs.Direction;

namespace Akka.Pathfinder;

public static partial class Conversions
{
    public static PathfinderRequest To(this Grpc.FindPathRequest value)
        => new(value.PathfinderId.To(), value.SourcePointId, value.TargetPointId, value.Direction.To(), value.Duration.ToTimeSpan());

    public static FindPathResponse To(this PathfinderResponse value)
        => new()
        {
            PathCost = 0, // todo: 
            PathfinderId = value.PathfinderId.ToString(),
            PathId = value.PathId.ToString(),
            Success = value.Success,
            ErrorMessage = string.Empty
        };

    public static PathPoint To(this Point value)
        => new(value.PointId, value.Cost, value.Direction.To());

    public static Guid To(this string value)
        => Guid.Parse(value);

    public static Direction To(this Grpc.Direction value)
        => value switch
        {
            Grpc.Direction.None => Direction.None,
            Grpc.Direction.Top => Direction.Top,
            Grpc.Direction.Bottom => Direction.Bottom,
            Grpc.Direction.Left => Direction.Left,
            Grpc.Direction.Right => Direction.Right,
            Grpc.Direction.Front => Direction.Front,
            Grpc.Direction.Back => Direction.Back,
            _ => Direction.None
        };

    public static Grpc.Direction To(this Direction value)
        => value switch
        {
            Direction.None => Grpc.Direction.None,
            Direction.Top => Grpc.Direction.Top,
            Direction.Bottom => Grpc.Direction.Bottom,
            Direction.Left => Grpc.Direction.Left,
            Direction.Right => Grpc.Direction.Right,
            Direction.Front => Grpc.Direction.Front,
            Direction.Back => Grpc.Direction.Back,
            _ => Grpc.Direction.None
        };
}
