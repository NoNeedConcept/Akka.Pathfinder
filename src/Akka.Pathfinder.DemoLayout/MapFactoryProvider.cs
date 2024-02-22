namespace Akka.Pathfinder.DemoLayout;

public class MapFactoryProvider : IMapFactoryProvider
{
    public static MapFactoryProvider Instance { get; } = new();


    public IMapFactory CreateFactory(FactoryType type = FactoryType.Default, Guid? mapId = null)
    {
        return type switch
        {
            FactoryType.GermanyRailway => new GermanRailwayNetworkFactory(mapId),
            _ => new MapFactory(mapId)
        };
    }
}

public enum FactoryType
{
    Default,
    GermanyRailway
}