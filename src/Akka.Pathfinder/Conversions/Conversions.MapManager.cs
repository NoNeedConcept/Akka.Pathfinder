
using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Grpc;

namespace Akka.Pathfinder;

public static partial class Conversions
{
    public static LoadMap ToLoadMap(this MapRequest mapRequest)
        => new(mapRequest.MapId.To());

    public static UpdateMap ToUpdateMap(this MapRequest mapRequest)
        => new(mapRequest.MapId.To());

    public static Core.Configs.PointConfig To(this PointConfig point)
        => new(point.Id, point.Cost, point.DirectionConfigs.ToDictionary(x => ((Direction)x.Key).To(), x => x.Value.To()), false);

    public static Core.Configs.DirectionConfig To(this DirectionConfig direction)
        => new(direction.TargetPointId, direction.Cost);

    public static Ack To(this MapLoaded _)
        => new() { Success = true };

    public static Ack To(this MapUpdated _)
        => new() { Success = true };
}