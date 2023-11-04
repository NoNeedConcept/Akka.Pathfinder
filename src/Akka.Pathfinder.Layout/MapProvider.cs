using Akka.Pathfinder.Core.Configs;

namespace Akka.Pathfinder.Layout;

//map 0
// dictionary 0, -> Map0()


public class MapProvider
{
    private const uint BaseCost = 42;

    public Dictionary<int, MapConfigWithPoints> MapConfigs => new()
        {
             {0, Map0()},
             {1, Map1()},
        };

    private MapConfigWithPoints Map0()
    {

        return new MapConfigWithPoints(Guid.NewGuid(), Guid.NewGuid(), new List<PointConfig>()
        {
            new(1, BaseCost, new Dictionary<Direction, DirectionConfig>()
            {
                { Direction.Front, new DirectionConfig(2, BaseCost) },
            }),
            new(2, BaseCost, new Dictionary<Direction, DirectionConfig>()),
        });

    }

    private MapConfigWithPoints Map1()
    {

        return new MapConfigWithPoints(Guid.NewGuid(), Guid.NewGuid(), new List<PointConfig>()
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