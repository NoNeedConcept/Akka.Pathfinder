namespace Akka.Pathfinder.DemoLayout;

public interface IMapFactoryProvider
{
    IMapFactory CreateFactory(FactoryType type = FactoryType.Default, Guid? mapId = null);
}