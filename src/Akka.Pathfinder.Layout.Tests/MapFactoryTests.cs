namespace Akka.Pathfinder.Layout.Tests;

public class MapFactoryTests
{
    [Fact]
    public void CreateRandomMapConfig()
    {
        int x = 50;
        var mapSize = new MapSize(x, x, x);
        var mapConfig = MapFactory.Create(new MapSettings(42, 20, mapSize, new Dictionary<Direction, uint>()
        {
            { Direction.Top, 100 },
            { Direction.Bottom, 100 }
        }));
        
        Assert.NotNull(mapConfig);
        Assert.NotEqual(Guid.Empty,mapConfig.Id);
        Assert.NotNull(mapConfig.Points);
        Assert.True(mapConfig.Points.Any());


    }
}