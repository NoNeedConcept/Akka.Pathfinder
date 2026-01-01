using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Grpc;
using MapStateResponse = Akka.Pathfinder.Grpc.MapStateResponse;

namespace Akka.Pathfinder;

public static partial class Conversions
{
    public static LoadMap ToLoadMap(this MapRequest mapRequest)
        => new(mapRequest.MapId.To());

    public static UpdateMap ToUpdateMap(this MapRequest mapRequest)
        => new(mapRequest.MapId.To());

    public static GetMapState ToGetMapState(this MapRequest mapRequest)
        => new(mapRequest.MapId.To());

    public static Core.Configs.PointConfig To(this PointConfig point)
        => new(point.Id, point.Cost,
            point.DirectionConfigs.ToDictionary(x => ((Direction)x.Key).To(), x => x.Value.To()), false);

    public static Core.Configs.DirectionConfig To(this DirectionConfig direction)
        => new(direction.TargetPointId, direction.Cost);

    public static MapStateResponse To(this Core.Messages.MapStateResponse response)
        => new() { MapId = response.MapId.ToString(), IsReady = response.IsReady };

    public static Ack To(this MapLoaded _)
        => new() { Success = true };

    public static Ack To(this MapUpdated _)
        => new() { Success = true };
}