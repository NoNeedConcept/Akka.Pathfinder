namespace Akka.Pathfinder;

public partial class Conversions
{
    public static Layout.MapSettings To(this Grpc.MapSettings settings)
        => new(settings.PointCost, settings.DefaultDirectionCost, new Layout.MapSize(settings.MapSize.Width, settings.MapSize.Height, settings.MapSize.Depth), [], new Random().Next(int.MinValue, int.MaxValue));
}
