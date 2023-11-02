using Akka.Pathfinder.Core.Configs;

namespace Akka.Pathfinder.Layout;

//map 0
// dictionary 0, -> Map0()


public static class MapProvider
{
    private const uint BaseCost = 42;
    private static Dictionary<int, MapConfig> _mapConfigs = new();


    public static Dictionary<int, MapConfig> MapConfigs
    {
        get
        {
            if (_mapConfigs.Count == 0)
            {
                _mapConfigs.Add(0, Map0());
                _mapConfigs.Add(1, Map1());
                _mapConfigs.Add(2,MapFactory.Create(new MapSettings(BaseCost,BaseCost*2,new MapSize(12, 15, 1),new Dictionary<Direction, uint>()),true));
                _mapConfigs.Add(3,MapFactory.Create(new MapSettings(BaseCost,BaseCost*2,new MapSize(50, 50, 1),new Dictionary<Direction, uint>()),true));
                _mapConfigs.Add(4,MapFactory.Create(new MapSettings(BaseCost,BaseCost*2,new MapSize(50, 50, 50),new Dictionary<Direction, uint>()),true));
            }

            return _mapConfigs;
        }
        set => _mapConfigs = value;
    }

    private static MapConfig Map0()
    {

        return new MapConfig(Guid.NewGuid(), new List<PointConfig>()
        {
            new(1, BaseCost, new Dictionary<Direction, DirectionConfig>()
            {
                { Direction.Front, new DirectionConfig(2, BaseCost) },
            }),
            new(2, BaseCost, new Dictionary<Direction, DirectionConfig>()),

        });

    }

    private static MapConfig Map1()
    {

        return new MapConfig(Guid.NewGuid(), new List<PointConfig>()
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
        });
    }
}