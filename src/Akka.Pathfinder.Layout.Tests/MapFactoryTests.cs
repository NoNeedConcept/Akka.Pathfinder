using Akka.Pathfinder.Core.Configs;

namespace Akka.Pathfinder.Layout.Tests;

public class MapFactoryTests
{
    [Fact]
    public void CreateRandomMapConfig()
    {
        int x = 3;
        var mapConfig = MapFactoryProvider.Instance.CreateFactory().Create(new MapSettings(42, 20, new MapSize(x, x, x), new Dictionary<Direction, uint>()
        {
            { Direction.Top, 100 },
            { Direction.Bottom, 100 }
        }), true);

        Assert.NotNull(mapConfig);
        Assert.NotEqual(Guid.Empty, mapConfig.Id);
        Assert.NotNull(mapConfig.CollectionIds);
        Assert.True(mapConfig.CollectionIds.Any());
        Assert.Equal(27, mapConfig.Configs.Values.SelectMany(x => x).Count());
    }

    [Fact]
    public void CreateMapConfigFromArray()
    {
        var mapSize = new MapSize(3, 3, 2);
        var mapSettings = new MapSettings(42, 20, mapSize, new Dictionary<Direction, uint>()
        {
            { Direction.Top, 100 },
            { Direction.Bottom, 100 }
        });

        var kekw = new Dictionary<int, int[,]>
        {
            { 0, new[,] { { 1, 1, 1 },
                          { 1, 0, 1 },
                          { 1, 1, 1 } } },

            { 1, new[,] { { 1, 1, 1 },
                          { 1, 1, 1 },
                          { 0, 0, 0 }
            } }
        };

        var mapConfig = MapFactoryProvider.Instance.CreateFactory().Create(mapSettings, kekw);

        Assert.NotNull(mapConfig);
        Assert.NotEqual(Guid.Empty, mapConfig.Id);
        Assert.NotNull(mapConfig.CollectionIds);
        Assert.True(mapConfig.CollectionIds.Any());
        Assert.Contains(mapConfig.Configs.Values, x => x.Any(t => t.Id == 1));
        Assert.Equal(3, mapConfig.Configs.Values.SelectMany(x => x).Single(x => x.Id == 1).DirectionConfigs.Count);
    }

     [Fact]
    public void CreateLargeMapConfig()
    {
        int x = 125;
        var mapConfig = MapFactoryProvider.Instance.CreateFactory().Create(new MapSettings(42, 20, new MapSize(x, x, x), new Dictionary<Direction, uint>()
        {
            { Direction.Top, 100 },
            { Direction.Bottom, 100 }
        }), true);

        Assert.NotNull(mapConfig);
        Assert.NotEqual(Guid.Empty, mapConfig.Id);
        Assert.NotNull(mapConfig.CollectionIds);
        Assert.True(mapConfig.CollectionIds.Any());
        Assert.Equal(1953125 , mapConfig.Configs.Values.SelectMany(x => x).Count());
    }
}