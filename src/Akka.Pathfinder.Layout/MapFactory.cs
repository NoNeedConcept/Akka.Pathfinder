using Akka.Pathfinder.Core.Configs;

namespace Akka.Pathfinder.Layout;

public class MongoConstantLengthForCollections
{
    public const int Length = 1500000;
}

public record MapSize(int Width, int Height, int Depth);

public record MapSettings(uint PointCost, uint DefaultDirectionCost, MapSize MapSize, Dictionary<Direction, uint> DirectionsCosts, int Seed = 0);

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
    MapConfigWithPoints Create(MapSettings mapSettings, IDictionary<int, int[,]> map);
}

public class MapFactory : IMapFactory
{
    private IDictionary<int, int[,]> Map { get; set; } = null!;
    private MapSettings MapSettings { get; set; } = null!;
    private Random Random { get; set; } = null!;
    private const int Point = 1;

    public MapConfigWithPoints Create(MapSettings mapSettings, bool intergalacticDummyMode = false)
    {
        MapSettings = mapSettings;
        InitializeRandom();
        InitializeDirectionCost();
        InitializeMap(intergalacticDummyMode);
        return ConvertToMapConfig();
    }

    public MapConfigWithPoints Create(MapSettings mapSettings, IDictionary<int, int[,]> map)
    {
        Map = map;
        MapSettings = mapSettings;
        InitializeDirectionCost();
        ConvertToIndexBasedMap();
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
        int index = 0;
        for (int depth = 0; depth < MapSettings.MapSize.Depth; depth++)
        {
            for (int height = 0; height < MapSettings.MapSize.Height; height++)
            {
                for (int width = 0; width < MapSettings.MapSize.Width; width++)
                {
                    if (Map[depth][width, height] != Point)
                    {
                        continue;
                    }

                    index++;

                    List<Direction> directionsToCheck = new();

                    if (width > 0) directionsToCheck.Add(Direction.Left);
                    if (height > 0) directionsToCheck.Add(Direction.Top);
                    if (depth > 0) directionsToCheck.Add(Direction.Back);

                    if (width < MapSettings.MapSize.Width - 1) directionsToCheck.Add(Direction.Right);
                    if (height < MapSettings.MapSize.Height - 1) directionsToCheck.Add(Direction.Bottom);
                    if (depth < MapSettings.MapSize.Depth - 1) directionsToCheck.Add(Direction.Front);


                    var tempDic = new Dictionary<Direction, DirectionConfig>();

                    foreach (Direction direction in directionsToCheck)
                    {
                        switch (direction)
                        {
                            case Direction.Top:
                                {
                                    if (Map[depth][width, height - 1] == Point)
                                    {
                                        tempDic.Add(direction,
                                            new DirectionConfig(Map[depth][width, height - 1],
                                                MapSettings.DirectionsCosts[direction]));
                                    }

                                    break;
                                }
                            case Direction.Bottom:
                                {
                                    if (Map[depth][width, height + 1] == Point)
                                    {
                                        tempDic.Add(direction,
                                            new DirectionConfig(Map[depth][width, height + 1],
                                                MapSettings.DirectionsCosts[direction]));
                                    }

                                    break;
                                }
                            case Direction.Left:
                                {
                                    if (Map[depth][width - 1, height] == Point)
                                    {
                                        tempDic.Add(direction,
                                            new DirectionConfig(Map[depth][width - 1, height],
                                                MapSettings.DirectionsCosts[direction]));
                                    }

                                    break;
                                }
                            case Direction.Right:
                                {
                                    if (Map[depth][width + 1, height] == Point)
                                    {
                                        tempDic.Add(direction,
                                            new DirectionConfig(Map[depth][width + 1, height],
                                                MapSettings.DirectionsCosts[direction]));
                                    }

                                    break;
                                }
                            case Direction.Front:
                                {
                                    if (Map[depth + 1][width, height] == Point)
                                    {
                                        tempDic.Add(direction,
                                            new DirectionConfig(Map[depth + 1][width, height],
                                                MapSettings.DirectionsCosts[direction]));
                                    }

                                    break;
                                }
                            case Direction.Back:
                                {
                                    if (Map[depth - 1][width, height] == Point)
                                    {
                                        tempDic.Add(direction,
                                            new DirectionConfig(Map[depth - 1][width, height],
                                                MapSettings.DirectionsCosts[direction]));
                                    }

                                    break;
                                }
                        }
                    }

                    listOfPoints.Add(new PointConfig(index, MapSettings.PointCost, tempDic));
                }
            }
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
        Map = new Dictionary<int, int[,]>();
        int index = 0;
        for (int width = 0; width < MapSettings.MapSize.Width; width++)
        {
            for (int height = 0; height < MapSettings.MapSize.Height; height++)
            {
                for (int depth = 0; depth < MapSettings.MapSize.Depth; depth++)
                {
                    index++;
                    if (intergalacticDummyMode)
                    {
                        Map[depth][width, height] = index;
                    }
                    else
                    {
                        Map[depth][width, height] = Random.Next(0, 2) == 1 ? index : 2;
                    }
                }
            }
        }
    }

    private void ConvertToIndexBasedMap()
    {
        int index = 0;
        for (int depth = 0; depth < MapSettings.MapSize.Depth; depth++)
        {
            for (int height = 0; height < MapSettings.MapSize.Height; height++)
            {
                for (int width = 0; width < MapSettings.MapSize.Width; width++)
                {
                    if (Map.TryGetValue(depth, out var ints))
                    {
                        index++;
                        Map[depth][width, height] = ints[width, height] switch
                        {
                            1 => index,
                            _ => 0,
                        };
                    }

                }
            }
        }
    }
}