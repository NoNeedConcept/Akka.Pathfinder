using Akka.Pathfinder.DemoLayout;
using Xunit.Abstractions;

namespace Akka.Pathfinder.Layout.Tests;

public class MapFactoryTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public MapFactoryTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void MapFactory_ShouldSkipZeroValues()
    {
        var mapSize = new MapSize(2, 2, 1);
        var mapSettings = new PredefinedMapSettings(42, 20, mapSize, []);

        var map = new Dictionary<int, int[,]>
        {
            {
                0, new[,]
                {
                    { 1, 0 },
                    { 0, 1 }
                }
            }
        };

        mapSettings = mapSettings with { PredefinedMap = map };

        var mapConfig = MapFactoryProvider.Instance.CreateFactory().Create(mapSettings);

        // Es sollten nur 2 Punkte sein (die 1er), nicht 4.
        Assert.Equal(2, mapConfig.Count);
    }

    [Fact]
    public void CreateRandomMapConfig()
    {
        const int x = 3;
        var mapConfig = MapFactoryProvider.Instance.CreateFactory().Create(new IntergalaticDummyMapSettings(42, 20,
            new MapSize(x, x, x), new Dictionary<Directions, uint>
            {
                { Directions.Top, 100 },
                { Directions.Bottom, 100 }
            }, IntergalacticDummyMode: true));

        Assert.NotNull(mapConfig);
        Assert.NotEqual(Guid.Empty, mapConfig.Id);
        Assert.NotNull(mapConfig.CollectionIds);
        Assert.True(mapConfig.CollectionIds.Count != 0);
        Assert.Equal(27, mapConfig.Configs.Values.SelectMany(x => x).Count());
    }

    [Fact]
    public void CreateMapConfigFromArray()
    {
        var mapSize = new MapSize(3, 3, 2);
        var mapSettings = new PredefinedMapSettings(42, 20, mapSize, new Dictionary<Directions, uint>
        {
            { Directions.Top, 100 },
            { Directions.Bottom, 100 }
        });

        var kekw = new Dictionary<int, int[,]>
        {
            {
                0, new[,]
                {
                    { 1, 1, 1 },
                    { 1, 0, 1 },
                    { 1, 1, 1 }
                }
            },

            {
                1, new[,]
                {
                    { 1, 1, 1 },
                    { 1, 1, 1 },
                    { 0, 0, 0 }
                }
            }
        };

        mapSettings = mapSettings with
        {
            PredefinedMap = kekw
        };

        var mapConfig = MapFactoryProvider.Instance.CreateFactory().Create(mapSettings);

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
        const int x = 125;
        var mapConfig = MapFactoryProvider
            .Instance
            .CreateFactory(mapId: Guid.Empty)
            .Create(new IntergalaticDummyMapSettings(42, 20,
                new MapSize(x, x, x), new Dictionary<Directions, uint>
                {
                    { Directions.Top, 100 },
                    { Directions.Bottom, 100 }
                },
                IntergalacticDummyMode: true));

        Assert.NotNull(mapConfig);
        Assert.Equal(Guid.Empty, mapConfig.Id);
        Assert.NotNull(mapConfig.CollectionIds);
        Assert.True(mapConfig.CollectionIds.Count != 0);
        Assert.Equal(1953125, mapConfig.Configs.Values.SelectMany(x => x).Count());
    }
}