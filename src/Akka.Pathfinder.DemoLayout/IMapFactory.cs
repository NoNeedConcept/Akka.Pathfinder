namespace Akka.Pathfinder.DemoLayout;

public interface IMapFactory
{
    MapConfigWithPoints Create(IMapSettings settings);
}