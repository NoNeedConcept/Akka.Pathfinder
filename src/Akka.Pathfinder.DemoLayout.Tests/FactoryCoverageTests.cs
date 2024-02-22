using Akka.Pathfinder.DemoLayout;

namespace Akka.Pathfinder.Layout.Tests;

public class FactoryCoverageTests
{
    [Fact]
    public void MapFactoryProvider_ShouldReturnCorrectFactories()
    {
        var provider = MapFactoryProvider.Instance;

        Assert.IsType<MapFactory>(provider.CreateFactory(FactoryType.Default));
        Assert.IsType<GermanRailwayNetworkFactory>(provider.CreateFactory(FactoryType.GermanyRailway));

        // Default parameter
        Assert.IsType<MapFactory>(provider.CreateFactory());
    }

    [Fact]
    public void MapFactory_ShouldThrowOnNullSettings()
    {
        var factory = MapFactoryProvider.Instance.CreateFactory(FactoryType.Default);
        Assert.Throws<ArgumentNullException>(() => factory.Create(null!));
    }

    [Fact]
    public void MapFactory_ShouldSupportRandomMode()
    {
        var factory = MapFactoryProvider.Instance.CreateFactory(FactoryType.Default);
        var settings = new IntergalaticDummyMapSettings(
            PointCost: 10,
            DefaultDirectionCost: 10,
            MapSize: new MapSize(5, 5, 1),
            DirectionsCosts: new Dictionary<Directions, uint>(),
            Seed: 42,
            IntergalacticDummyMode: false
        );

        var mapConfig = factory.Create(settings);
        Assert.NotNull(mapConfig);
    }

    [Fact]
    public void MapFactory_LargeMap_ShouldTriggerChunking()
    {
        // We need more than 1.500.000 points to trigger chunking
        // 100 * 100 * 151 = 1.510.000
        var factory = MapFactoryProvider.Instance.CreateFactory(FactoryType.Default);
        var settings = new IntergalaticDummyMapSettings(
            PointCost: 10,
            DefaultDirectionCost: 10,
            MapSize: new MapSize(100, 100, 151),
            DirectionsCosts: new Dictionary<Directions, uint>(),
            Seed: 42,
            IntergalacticDummyMode: true
        );

        var mapConfig = factory.Create(settings);

        Assert.True(mapConfig.CollectionIds.Count > 1);
    }
}