using System.Diagnostics;
using Akka.Pathfinder.Core.Configs;
using LanguageExt.Pipes;
using Serilog;

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
    public MapConfigWithPoints Create(MapSettings mapSettings, bool intergalacticDummyMode = false)
    {
        var random = InitializeRandom(mapSettings);
        InitializeDirectionCost(ref mapSettings);
        var map = InitializeMap(mapSettings, random, intergalacticDummyMode);
        return ConvertToMapConfig(map, mapSettings);
    }

    public MapConfigWithPoints Create(MapSettings mapSettings, IDictionary<int, int[,]> map)
    {
        InitializeDirectionCost(ref mapSettings);
        var indexBasedMap = ConvertToIndexBasedMap(map);
        return ConvertToMapConfig(indexBasedMap, mapSettings);
    }

    private static Random InitializeRandom(MapSettings? settings)
    {
        int seedToUse = settings?.Seed ?? 0;

        if (seedToUse == 0)
        {
            seedToUse = DateTime.UtcNow.Microsecond;
        }

        return new Random(seedToUse);
    }

    private static void InitializeDirectionCost(ref MapSettings settings)
    {
        if (!settings.DirectionsCosts.ContainsKey(Direction.Back))
        {
            settings.DirectionsCosts.Add(Direction.Back, settings.DefaultDirectionCost);
        }

        if (!settings.DirectionsCosts.ContainsKey(Direction.Front))
        {
            settings.DirectionsCosts.Add(Direction.Front, settings.DefaultDirectionCost);
        }

        if (!settings.DirectionsCosts.ContainsKey(Direction.Left))
        {
            settings.DirectionsCosts.Add(Direction.Left, settings.DefaultDirectionCost);
        }

        if (!settings.DirectionsCosts.ContainsKey(Direction.Right))
        {
            settings.DirectionsCosts.Add(Direction.Right, settings.DefaultDirectionCost);
        }

        if (!settings.DirectionsCosts.ContainsKey(Direction.Bottom))
        {
            settings.DirectionsCosts.Add(Direction.Bottom, settings.DefaultDirectionCost);
        }

        if (!settings.DirectionsCosts.ContainsKey(Direction.Top))
        {
            settings.DirectionsCosts.Add(Direction.Top, settings.DefaultDirectionCost);
        }
    }

    private static MapConfigWithPoints ConvertToMapConfig(IDictionary<int, int[,]> map, MapSettings settings)
    {
        var listOfPoints = new List<PointConfig>();
        int index = 0;
        for (int depth = 0; depth < map.Count; depth++)
        {
            for (int height = 0; height < map[depth].GetLength(0); height++)
            {
                for (int width = 0; width < map[depth].GetLength(1); width++)
                {
                    if (map[depth][width, height] == 0)
                    {
                        continue;
                    }

                    index++;
                    List<Direction> directionsToCheck = new();

                    if (width > 0) directionsToCheck.Add(Direction.Left);
                    if (height > 0) directionsToCheck.Add(Direction.Front);
                    if (depth > 0) directionsToCheck.Add(Direction.Bottom);

                    if (width < map.Count) directionsToCheck.Add(Direction.Right);
                    if (height < map[depth].GetLength(0)) directionsToCheck.Add(Direction.Back);
                    if (depth < map[depth].GetLength(1)) directionsToCheck.Add(Direction.Top);

                    var tempDic = new Dictionary<Direction, DirectionConfig>();

                    foreach (Direction direction in directionsToCheck)
                    {
                        switch (direction)
                        {
                            case Direction.Front:
                                {
                                    if (map.TryGetValue(depth, out var values) &&
                                        values.GetLength(0) > height - 1 &&
                                        values.GetLength(1) > width)
                                    {
                                        if (height - 1 <= -1 || width <= -1) continue;
                                        if (values[width, height - 1] == 0) continue;
                                        tempDic.Add(direction,
                                            new DirectionConfig(map[depth][width, height - 1],
                                                settings.DirectionsCosts[direction]));
                                    }

                                    break;
                                }
                            case Direction.Back:
                                {
                                    if (map.TryGetValue(depth, out var values) &&
                                        values.GetLength(0) > height + 1 &&
                                        values.GetLength(1) > width)
                                    {
                                        if (height + 1 <= -1 || width <= -1) continue;
                                        if (values[width, height + 1] == 0) continue;
                                        tempDic.Add(direction,
                                            new DirectionConfig(map[depth][width, height + 1],
                                                settings.DirectionsCosts[direction]));
                                    }

                                    break;
                                }
                            case Direction.Left:
                                {
                                    if (map.TryGetValue(depth, out var values) &&
                                        values.GetLength(0) > height &&
                                        values.GetLength(1) > width - 1)
                                    {

                                        tempDic.Add(direction,
                                            new DirectionConfig(map[depth][width - 1, height],
                                                settings.DirectionsCosts[direction]));
                                    }

                                    break;
                                }
                            case Direction.Right:
                                {
                                    if (map.TryGetValue(depth, out var values) &&
                                        values.GetLength(0) > height &&
                                        values.GetLength(1) > width + 1)
                                    {
                                        if (height <= -1 || width + 1 <= -1) continue;
                                        if (values[width + 1, height] == 0) continue;
                                        tempDic.Add(direction,
                                            new DirectionConfig(map[depth][width + 1, height],
                                                settings.DirectionsCosts[direction]));
                                    }

                                    break;
                                }
                            case Direction.Top:
                                {
                                    if (map.TryGetValue(depth + 1, out var values) &&
                                        values.GetLength(0) > height &&
                                        values.GetLength(1) > width)
                                    {
                                        if (height <= -1 || width <= -1) continue;
                                        if (values[width, height] == 0) continue;
                                        tempDic.Add(direction,
                                            new DirectionConfig(map[depth + 1][width, height],
                                                settings.DirectionsCosts[direction]));
                                    }

                                    break;
                                }
                            case Direction.Bottom:
                                {
                                    if (map.TryGetValue(depth - 1, out var values) &&
                                        values.GetLength(0) > height &&
                                        values.GetLength(1) > width)
                                    {
                                        if (height <= -1 || width <= -1) continue;
                                        if (values[width, height] == 0) continue;
                                        tempDic.Add(direction,
                                            new DirectionConfig(map[depth - 1][width, height],
                                                settings.DirectionsCosts[direction]));
                                    }

                                    break;
                                }
                        }
                    }

                    listOfPoints.Add(new PointConfig(index, settings.PointCost, tempDic));
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
                { Guid.NewGuid(), listOfPoints }
            };
        }

        return new MapConfigWithPoints(Guid.NewGuid(), result);
    }

    private static IDictionary<int, int[,]> InitializeMap(MapSettings settings, Random? random = default, bool intergalacticDummyMode = false)
    {
        var result = new Dictionary<int, int[,]>();
        for (int depth = 0; depth < settings.MapSize.Depth; depth++)
        {
            result.Add(depth, new int[settings.MapSize.Width, settings.MapSize.Height]);
        }

        int index = 0;
        for (int depth = 0; depth < settings.MapSize.Depth; depth++)
        {
            for (int height = 0; height < result[depth].GetLength(0); height++)
            {
                for (int width = 0; width < result[depth].GetLength(1); width++)
                {
                    index++;
                    if (intergalacticDummyMode)
                    {
                        result[depth][width, height] = index;
                    }
                    else
                    {
                        result[depth][width, height] = random?.Next(0, 2) == 1 ? index : 2;
                    }
                }
            }
        }

        return result;
    }

    private static IDictionary<int, int[,]> ConvertToIndexBasedMap(IDictionary<int, int[,]> map)
    {
        int index = 0;
        for (int depth = 0; depth < map.Count; depth++)
        {
            Console.WriteLine("Level {0}", depth);
            for (int height = 0; height < map[depth].GetLength(0); height++)
            {
                for (int width = 0; width < map[depth].GetLength(1); width++)
                {
                    if (map.TryGetValue(depth, out var ints))
                    {
                        index++;

                        var result = ints[width, height] switch
                        {
                            1 => index,
                            _ => 0,
                        };
                        Console.Write(string.Format("{0} ", result));
                        map[depth][width, height] = result;
                    }
                }

                Console.Write(Environment.NewLine + Environment.NewLine);
            }
        }

        return map;
    }
}