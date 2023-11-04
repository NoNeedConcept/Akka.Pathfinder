using Akka.Pathfinder.Core.Configs;
using LanguageExt.UnitsOfMeasure;

namespace Akka.Pathfinder.Layout;

public class MongoConstantLengthForCollections
{
    public const int Length = 1500000;
}

public record MapSize(int Width, int Height, int Depth);

public record MapSettings(uint PointCost, uint DefaultDirectionCost, MapSize MapSize,
    Dictionary<Direction, uint> DirectionsCosts, int Seed = 0);


public interface IMapFactoryProvider
{
    IMapFactory CreateFactory();
}

public class MapFactoryProvider : IMapFactoryProvider
{
    public static MapFactoryProvider Instance { get; } = new();

    public IMapFactory CreateFactory() => new MapFactory();
}

public interface IMapFactory
{
    MapConfigWithPoints Create(MapSettings mapSettings, bool intergalacticDummyMode = false);
}

public class MapFactory : IMapFactory
{
    private int[,,] Map { get; set; } = null!;
    private MapSettings MapSettings { get; set; } = null!;
    private Random Random { get; set; } = null!;

    private const int EmptyPoint = -1;

    public MapConfigWithPoints Create(MapSettings mapSettings, bool intergalacticDummyMode = false)
    {
        MapSettings = mapSettings;
        InitializeRandom();
        InitializeDirectionCost();
        InitializeMap(intergalacticDummyMode);
        return ConvertToMapConfig();
    }

    private void InitializeRandom()
    {
        int seedToUse = MapSettings.Seed;

        if (seedToUse == 0)
        {
            seedToUse = DateTime.UtcNow.Microsecond;
        }

        Random = new Random(seedToUse);
    }

    private void InitializeDirectionCost()
    {
        if (!MapSettings.DirectionsCosts.ContainsKey(Direction.Back))
        {
            MapSettings.DirectionsCosts.Add(Direction.Back, MapSettings.DefaultDirectionCost);
        }

        if (!MapSettings.DirectionsCosts.ContainsKey(Direction.Front))
        {
            MapSettings.DirectionsCosts.Add(Direction.Front, MapSettings.DefaultDirectionCost);
        }

        if (!MapSettings.DirectionsCosts.ContainsKey(Direction.Left))
        {
            MapSettings.DirectionsCosts.Add(Direction.Left, MapSettings.DefaultDirectionCost);
        }

        if (!MapSettings.DirectionsCosts.ContainsKey(Direction.Right))
        {
            MapSettings.DirectionsCosts.Add(Direction.Right, MapSettings.DefaultDirectionCost);
        }

        if (!MapSettings.DirectionsCosts.ContainsKey(Direction.Bottom))
        {
            MapSettings.DirectionsCosts.Add(Direction.Bottom, MapSettings.DefaultDirectionCost);
        }

        if (!MapSettings.DirectionsCosts.ContainsKey(Direction.Top))
        {
            MapSettings.DirectionsCosts.Add(Direction.Top, MapSettings.DefaultDirectionCost);
        }
    }

    private MapConfigWithPoints ConvertToMapConfig()
    {
        var listOfPoints = new List<PointConfig>();
        int widthCounter = 0, heightCounter = 0, depthCounter = 0;
        int index = 0;
        while (widthCounter < MapSettings.MapSize.Width)
        {
            while (heightCounter < MapSettings.MapSize.Height)
            {
                while (depthCounter < MapSettings.MapSize.Depth)
                {
                    if (Map[widthCounter, heightCounter, depthCounter] == EmptyPoint)
                    {
                        depthCounter++;
                        continue;
                    }

                    index++;

                    List<Direction> directionsToCheck = new List<Direction>();

                    if (widthCounter > 0) directionsToCheck.Add(Direction.Left);
                    if (heightCounter > 0) directionsToCheck.Add(Direction.Top);
                    if (depthCounter > 0) directionsToCheck.Add(Direction.Back);

                    if (widthCounter < MapSettings.MapSize.Width - 1) directionsToCheck.Add(Direction.Right);
                    if (heightCounter < MapSettings.MapSize.Height - 1) directionsToCheck.Add(Direction.Bottom);
                    if (depthCounter < MapSettings.MapSize.Depth - 1) directionsToCheck.Add(Direction.Front);


                    var tempDic = new Dictionary<Direction, DirectionConfig>();

                    foreach (Direction direction in directionsToCheck)
                    {
                        switch (direction)
                        {
                            case Direction.Top:
                                {
                                    if (Map[widthCounter, heightCounter - 1, depthCounter] != EmptyPoint)
                                    {
                                        tempDic.Add(direction,
                                            new DirectionConfig(Map[widthCounter, heightCounter - 1, depthCounter],
                                                MapSettings.DirectionsCosts[direction]));
                                    }

                                    break;
                                }
                            case Direction.Bottom:
                                {
                                    if (Map[widthCounter, heightCounter + 1, depthCounter] != EmptyPoint)
                                    {
                                        tempDic.Add(direction,
                                            new DirectionConfig(Map[widthCounter, heightCounter + 1, depthCounter],
                                                MapSettings.DirectionsCosts[direction]));
                                    }

                                    break;
                                }
                            case Direction.Left:
                                {
                                    if (Map[widthCounter - 1, heightCounter, depthCounter] != EmptyPoint)
                                    {
                                        tempDic.Add(direction,
                                            new DirectionConfig(Map[widthCounter - 1, heightCounter, depthCounter],
                                                MapSettings.DirectionsCosts[direction]));
                                    }

                                    break;
                                }
                            case Direction.Right:
                                {
                                    if (Map[widthCounter + 1, heightCounter, depthCounter] != EmptyPoint)
                                    {
                                        tempDic.Add(direction,
                                            new DirectionConfig(Map[widthCounter + 1, heightCounter, depthCounter],
                                                MapSettings.DirectionsCosts[direction]));
                                    }

                                    break;
                                }
                            case Direction.Front:
                                {
                                    if (Map[widthCounter, heightCounter, depthCounter + 1] != EmptyPoint)
                                    {
                                        tempDic.Add(direction,
                                            new DirectionConfig(Map[widthCounter, heightCounter, depthCounter + 1],
                                                MapSettings.DirectionsCosts[direction]));
                                    }

                                    break;
                                }
                            case Direction.Back:
                                {
                                    if (Map[widthCounter, heightCounter, depthCounter - 1] != EmptyPoint)
                                    {
                                        tempDic.Add(direction,
                                            new DirectionConfig(Map[widthCounter, heightCounter, depthCounter - 1],
                                                MapSettings.DirectionsCosts[direction]));
                                    }

                                    break;
                                }
                        }
                    }

                    listOfPoints.Add(new PointConfig(index, MapSettings.PointCost, tempDic));

                    depthCounter++;
                }

                depthCounter = 0;
                heightCounter++;
            }


            heightCounter = 0;
            widthCounter++;
        }

        var result = new Dictionary<Guid, List<PointConfig>>();
        if (listOfPoints.Count > MongoConstantLengthForCollections.Length)
        {
            var kekw = listOfPoints
            .Chunk(MongoConstantLengthForCollections.Length)
            .Select(x => new KeyValuePair<Guid, List<PointConfig>>(Guid.NewGuid(), x.ToList()))
            .ToDictionary(x => x.Key, x => x.Value);
            result = kekw;
        }
        else
        {
            result = new Dictionary<Guid, List<PointConfig>>()
            {
                {Guid.NewGuid(), listOfPoints}
            };
        }

        return new MapConfigWithPoints(Guid.NewGuid(), result);
    }

    private void InitializeMap(bool intergalacticDummyMode)
    {
        Map = new int[MapSettings.MapSize.Width, MapSettings.MapSize.Height, MapSettings.MapSize.Depth];
        int widthCounter = 0, heightCounter = 0, depthCounter = 0;
        int index = 0;
        while (widthCounter < MapSettings.MapSize.Width)
        {
            while (heightCounter < MapSettings.MapSize.Height)
            {
                while (depthCounter < MapSettings.MapSize.Depth)
                {
                    index++;
                    if (intergalacticDummyMode)
                    {
                        Map[widthCounter, heightCounter, depthCounter] = index;
                    }
                    else
                    {
                        Map[widthCounter, heightCounter, depthCounter] =
                            Random.Next(0, 2) == 1 ? index : EmptyPoint;
                    }

                    depthCounter++;
                }

                depthCounter = 0;
                heightCounter++;
            }


            heightCounter = 0;
            widthCounter++;
        }
    }
}