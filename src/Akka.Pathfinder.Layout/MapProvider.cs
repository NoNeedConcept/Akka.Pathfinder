using Akka.Pathfinder.Core.Configs;

namespace Akka.Pathfinder.Layout;

public class MapProvider
{
    private const uint BaseCost = 42;

    private static readonly MapFactoryProvider _factoryProvider = MapFactoryProvider.Instance;

    public Dictionary<int, MapConfigWithPoints> MapConfigs => new()
        {
            { 0, Map0()},
            { 1, Map1()},
            { 2, _factoryProvider.CreateFactory().Create(new MapSettings(BaseCost, BaseCost*2, new MapSize(3, 3, 3), [], 15), true)},
            { 3, _factoryProvider.CreateFactory().Create(new MapSettings(BaseCost, BaseCost*2, new MapSize(15, 15, 15), [], 20), true)},
            { 4, _factoryProvider.CreateFactory().Create(new MapSettings(BaseCost, BaseCost*2, new MapSize(25, 25, 25), [], 20), true)}
        };

    private static MapConfigWithPoints Map0()
    {
        var value = new List<PointConfig>(){
                new(1, BaseCost, new Dictionary<Direction, DirectionConfig>()
                {
                    { Direction.Front, new DirectionConfig(2, BaseCost) },
                }),
                new(2, BaseCost, []),
            };
        return new MapConfigWithPoints(Guid.NewGuid(), new Dictionary<Guid, List<PointConfig>>() { { Guid.NewGuid(), value } });
    }

    private static MapConfigWithPoints Map1()
    {
        return new MapConfigWithPoints(Guid.NewGuid(), new Dictionary<Guid, List<PointConfig>>(){{ Guid.NewGuid(), new List<PointConfig>()
        {
            new(1, BaseCost, new Dictionary<Direction, DirectionConfig>()
            {
                { Direction.Left, new DirectionConfig(8, BaseCost) },
            }),
            new(2, BaseCost, new Dictionary<Direction, DirectionConfig>()
            {
                { Direction.Bottom, new DirectionConfig(1, BaseCost) },
                { Direction.Left, new DirectionConfig(9, BaseCost) },
            }),
            new(3, BaseCost, new Dictionary<Direction, DirectionConfig>()
            {
                { Direction.Bottom, new DirectionConfig(2, BaseCost) },
            }),
            new(4, BaseCost, new Dictionary<Direction, DirectionConfig>()
            {
                { Direction.Right, new DirectionConfig(3, BaseCost) },
                { Direction.Left, new DirectionConfig(5, BaseCost) },
            }),
            new(5, BaseCost, new Dictionary<Direction, DirectionConfig>()
            {
                { Direction.Bottom, new DirectionConfig(5, BaseCost) },
            }),
            new(6, BaseCost, new Dictionary<Direction, DirectionConfig>()
            {
                { Direction.Bottom, new DirectionConfig(7, BaseCost) },
                { Direction.Right, new DirectionConfig(9, BaseCost) },
            }),
            new(7,BaseCost, new Dictionary<Direction, DirectionConfig>()
            {
                { Direction.Right, new DirectionConfig(8, BaseCost) },
            }),
            new(8, BaseCost,new Dictionary<Direction, DirectionConfig>()
            {
                { Direction.Top, new DirectionConfig(9, BaseCost) },
            }),
            new(9, BaseCost, new Dictionary<Direction, DirectionConfig>()
            {
                { Direction.Top, new DirectionConfig(4, BaseCost) },
            })
        }}});
    }
}