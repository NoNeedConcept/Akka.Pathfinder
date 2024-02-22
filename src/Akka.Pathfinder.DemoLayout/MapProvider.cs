namespace Akka.Pathfinder.DemoLayout;

public static class MapProvider
{
    private const uint BaseCost = 42;

    private static readonly MapFactoryProvider FactoryProvider = MapFactoryProvider.Instance;

    public static Dictionary<int, MapConfigWithPoints> MapConfigs => new()
    {
        { 0, Map0() },
        { 1, Map1() },
        {
            2,
            FactoryProvider.CreateFactory(mapId: new Guid("23cdf167-02a1-47c4-9159-0387476f9670"))
                .Create(new IntergalaticDummyMapSettings(BaseCost, BaseCost * 2, new MapSize(3, 3, 3), [], 15, true))
        },
        {
            3,
            FactoryProvider.CreateFactory(mapId: new Guid("a89e0972-bb85-4a0e-9d00-87dbfb063170")).Create(
                new IntergalaticDummyMapSettings(BaseCost, BaseCost * 2, new MapSize(15, 15, 15), [], 20, true))
        },
        {
            6,
            FactoryProvider
                .CreateFactory(
                    type: FactoryType.GermanyRailway,
                    mapId: new Guid("789D86BA-795B-468B-9FCD-DD75CC3E90C7"))
                .Create(new GermanRailwayNetworkSettings(
                    Scale: 2,
                    IncludeMetro: true,
                    IncludeTram: true,
                    IncludeRegionalLines: true))
                .RemoveIsolatedPoints()
        },
        {
            7,
            FactoryProvider
                .CreateFactory(
                    type: FactoryType.GermanyRailway,
                    mapId: new Guid("789D86BA-795B-468B-9FCD-DD75CC3E90C8"))
                .Create(new GermanRailwayNetworkSettings(
                    Scale: 1,
                    Detail: DetailLevel.Low,
                    IncludeRegionalLines: true))
                .RemoveIsolatedPoints()
        },
        {
            8,
            FactoryProvider
                .CreateFactory(
                    type: FactoryType.GermanyRailway,
                    mapId: new Guid("789D86BA-795B-468B-9FCD-DD75CC3E90C9"))
                .Create(new GermanRailwayNetworkSettings(
                    Scale: 1,
                    Detail: DetailLevel.High,
                    IncludeRegionalLines: true))
                .RemoveIsolatedPoints()
        }
    };

    private static MapConfigWithPoints Map0()
    {
        var value = new List<PointConfig>
        {
            new(1, BaseCost, new Dictionary<Directions, DirectionConfig>
            {
                { Directions.Front, new DirectionConfig(2, BaseCost) },
            }),
            new(2, BaseCost, []),
        };
        return new MapConfigWithPoints(new Guid("23ecfc6d-3194-4c7a-8b4e-477b1c0d9150"),
            new Dictionary<Guid, List<PointConfig>> { { Guid.NewGuid(), value } }, 2, 1, 1);
    }

    private static MapConfigWithPoints Map1()
    {
        return new MapConfigWithPoints(new Guid("52259b19-eacf-4a33-a542-4dc7c7a18585"),
            new Dictionary<Guid, List<PointConfig>>
            {
                {
                    Guid.NewGuid(), [
                        new(1, BaseCost, new Dictionary<Directions, DirectionConfig>
                        {
                            { Directions.Left, new DirectionConfig(8, BaseCost) },
                        }),

                        new(2, BaseCost, new Dictionary<Directions, DirectionConfig>
                        {
                            { Directions.Bottom, new DirectionConfig(1, BaseCost) },
                            { Directions.Left, new DirectionConfig(9, BaseCost) },
                        }),

                        new(3, BaseCost, new Dictionary<Directions, DirectionConfig>
                        {
                            { Directions.Bottom, new DirectionConfig(2, BaseCost) },
                        }),

                        new(4, BaseCost, new Dictionary<Directions, DirectionConfig>
                        {
                            { Directions.Right, new DirectionConfig(3, BaseCost) },
                            { Directions.Left, new DirectionConfig(5, BaseCost) },
                        }),

                        new(5, BaseCost, new Dictionary<Directions, DirectionConfig>
                        {
                            { Directions.Bottom, new DirectionConfig(5, BaseCost) },
                        }),

                        new(6, BaseCost, new Dictionary<Directions, DirectionConfig>
                        {
                            { Directions.Bottom, new DirectionConfig(7, BaseCost) },
                            { Directions.Right, new DirectionConfig(9, BaseCost) },
                        }),

                        new(7, BaseCost, new Dictionary<Directions, DirectionConfig>
                        {
                            { Directions.Right, new DirectionConfig(8, BaseCost) },
                        }),

                        new(8, BaseCost, new Dictionary<Directions, DirectionConfig>
                        {
                            { Directions.Top, new DirectionConfig(9, BaseCost) },
                        }),

                        new(9, BaseCost, new Dictionary<Directions, DirectionConfig>
                        {
                            { Directions.Top, new DirectionConfig(4, BaseCost) },
                        })
                    ]
                }
            }, 3, 3, 1);
    }
}