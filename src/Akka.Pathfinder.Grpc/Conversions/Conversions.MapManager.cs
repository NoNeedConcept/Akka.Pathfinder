using Akka.Pathfinder.Core.Messages;

namespace Akka.Pathfinder.Grpc.Conversions;

public static partial class Conversions
{
    public static LoadMap ToLoadMap(this MapRequest mapRequest)
        => new(mapRequest.MapId.To());

    public static GetMapState ToGetMapState(this MapRequest mapRequest)
        => new(mapRequest.MapId.To());

    public static DeleteMap ToDeleteMap(this MapRequest mapRequest)
        => new(mapRequest.MapId.To());

    public static Core.Configs.PointConfig To(this PointConfig point)
        => new(point.Id, point.Cost,
            point.DirectionConfigs.ToDictionary(x => ((Direction)x.Key).To(), x => x.Value.To()), false);

    public static Core.Configs.DirectionConfig To(this DirectionConfig direction)
        => new(direction.TargetPointId, direction.Cost);

    public static StateResponse To(this MapStateResponse response)
        => new() { MapId = response.MapId.ToString(), IsReady = response.IsReady };

    public static DeleteResponse To(this MapDeleted response)
        => new() { Success = response.Success, ErrorMessage = response.ErrorMessage ?? string.Empty };

    public static Ack To(this MapLoaded response)
        => new() { Success = response.Success };
}