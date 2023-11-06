namespace Akka.Pathfinder.Layout.Tests;

public class MapFactoryTests
{
    [Fact]
    public void CreateRandomMapConfig()
    {
        int x = 50;
        var mapSize = new MapSize(x, x, 1);
        var mapConfig = MapFactoryProvider.Instance.CreateFactory().Create(new MapSettings(42, 20, mapSize, new Dictionary<Direction, uint>()
        {
            { Direction.Top, 100 },
            { Direction.Bottom, 100 }
        }));

        Assert.NotNull(mapConfig);
        Assert.NotEqual(Guid.Empty, mapConfig.Id);
        Assert.NotNull(mapConfig.PointConfigsIds);
        Assert.True(mapConfig.PointConfigsIds.Any());
    }

    [Fact]
    public void CreateMapConfigFromArray()
    {
        var mapSize = new MapSize(5, 3, 2);
        var mapSettings = new MapSettings(42, 20, mapSize, new Dictionary<Direction, uint>()
        {
            { Direction.Top, 100 },
            { Direction.Bottom, 100 }
        });

        var kekw = new Dictionary<int, int[,]>
        {
            { 1, new[,] { { 1, 1, 1 },
                          { 1, 0, 1 },
                          { 1, 1, 1 } } },

            { 2, new[,] { { 1, 1, 1 },
                          { 1, 1, 1 } } }
        };

        var mapConfig = MapFactoryProvider.Instance.CreateFactory().Create(mapSettings, kekw);

        Assert.NotNull(mapConfig);
        Assert.NotEqual(Guid.Empty, mapConfig.Id);
        Assert.NotNull(mapConfig.PointConfigsIds);
        Assert.True(mapConfig.PointConfigsIds.Any());
    }
}