namespace Akka.Pathfinder.DemoLayout;

public static class MongoConstantLengthForCollections
{
    public const int Length = 1500000;
}

public class MapFactory : IMapFactory
{
    private readonly Guid _mapId;

    internal MapFactory(Guid? mapId = null)
    {
        _mapId = mapId ?? Guid.NewGuid();
    }

    public MapConfigWithPoints Create(IMapSettings settings)
    {
        var mapSettings = settings as MapSettings ?? null;
        ArgumentNullException.ThrowIfNull(mapSettings);
        
        var random = InitializeRandom(mapSettings);
        InitializeDirectionCost(ref mapSettings);
        var map = settings switch
        {
            IntergalaticDummyMapSettings dummyMapSettings => InitializeMap(mapSettings, random,
                dummyMapSettings.IntergalacticDummyMode),
            PredefinedMapSettings predefinedMapSettings => predefinedMapSettings.PredefinedMap!,
            _ => null
        };

        ArgumentNullException.ThrowIfNull(map);
        var indexBasedMap = ConvertToIndexBasedMap(map);
        return ConvertToMapConfig(indexBasedMap, mapSettings);
    }

    private static Random InitializeRandom(MapSettings? settings)
    {
        var seedToUse = settings?.Seed ?? 0;

        if (seedToUse == 0)
        {
            seedToUse = DateTime.UtcNow.Microsecond;
        }

        return new Random(seedToUse);
    }

    private static void InitializeDirectionCost(ref MapSettings settings)
    {
        if (!settings.DirectionsCosts.ContainsKey(Directions.Back))
        {
            settings.DirectionsCosts.Add(Directions.Back, settings.DefaultDirectionCost);
        }

        if (!settings.DirectionsCosts.ContainsKey(Directions.Front))
        {
            settings.DirectionsCosts.Add(Directions.Front, settings.DefaultDirectionCost);
        }

        if (!settings.DirectionsCosts.ContainsKey(Directions.Left))
        {
            settings.DirectionsCosts.Add(Directions.Left, settings.DefaultDirectionCost);
        }

        if (!settings.DirectionsCosts.ContainsKey(Directions.Right))
        {
            settings.DirectionsCosts.Add(Directions.Right, settings.DefaultDirectionCost);
        }

        if (!settings.DirectionsCosts.ContainsKey(Directions.Bottom))
        {
            settings.DirectionsCosts.Add(Directions.Bottom, settings.DefaultDirectionCost);
        }

        if (!settings.DirectionsCosts.ContainsKey(Directions.Top))
        {
            settings.DirectionsCosts.Add(Directions.Top, settings.DefaultDirectionCost);
        }
    }

    private MapConfigWithPoints ConvertToMapConfig(IDictionary<int, int[,]> map, MapSettings settings)
    {
        var listOfPoints = new List<PointConfig>();
        var index = 0;
        for (var depth = 0; depth < map.Count; depth++)
        {
            for (var height = 0; height < map[depth].GetLength(0); height++)
            {
                for (var width = 0; width < map[depth].GetLength(1); width++)
                {
                    if (map[depth][height, width] == -1)
                    {
                        continue;
                    }

                    index++;
                    List<Directions> directionsToCheck = [];

                    if (width > 0) directionsToCheck.Add(Directions.Left);
                    if (height > 0) directionsToCheck.Add(Directions.Front);
                    if (depth > 0) directionsToCheck.Add(Directions.Bottom);

                    if (height < map[depth].GetLength(0) - 1) directionsToCheck.Add(Directions.Back);
                    if (width < map[depth].GetLength(1) - 1) directionsToCheck.Add(Directions.Right);
                    if (depth < map.Count - 1) directionsToCheck.Add(Directions.Top);

                    var tempDic = new Dictionary<Directions, DirectionConfig>();

                    foreach (var direction in directionsToCheck)
                    {
                        switch (direction)
                        {
                            case Directions.Front:
                            {
                                if (map.TryGetValue(depth, out var values) &&
                                    values.GetLength(0) > height - 1 &&
                                    values.GetLength(1) > width)
                                {
                                    if (height - 1 <= -1 || width <= -1) continue;
                                    if (values[height - 1, width] == -1) continue;
                                    tempDic.Add(direction,
                                        new DirectionConfig(map[depth][height - 1, width],
                                            settings.DirectionsCosts[direction]));
                                }

                                break;
                            }
                            case Directions.Back:
                            {
                                if (map.TryGetValue(depth, out var values) &&
                                    values.GetLength(0) > height + 1 &&
                                    values.GetLength(1) > width)
                                {
                                    if (height + 1 <= -1 || width <= -1) continue;
                                    if (values[height + 1, width] == -1) continue;
                                    tempDic.Add(direction,
                                        new DirectionConfig(map[depth][height + 1, width],
                                            settings.DirectionsCosts[direction]));
                                }

                                break;
                            }
                            case Directions.Left:
                            {
                                if (map.TryGetValue(depth, out var values) &&
                                    values.GetLength(0) > height &&
                                    values.GetLength(1) > width - 1)
                                {
                                    if (height <= -1 || width - 1 <= -1) continue;
                                    if (values[height, width - 1] == -1) continue;
                                    tempDic.Add(direction,
                                        new DirectionConfig(map[depth][height, width - 1],
                                            settings.DirectionsCosts[direction]));
                                }

                                break;
                            }
                            case Directions.Right:
                            {
                                if (map.TryGetValue(depth, out var values) &&
                                    values.GetLength(0) > height &&
                                    values.GetLength(1) > width + 1)
                                {
                                    if (height <= -1 || width + 1 <= -1) continue;
                                    if (values[height, width + 1] == -1) continue;
                                    tempDic.Add(direction,
                                        new DirectionConfig(map[depth][height, width + 1],
                                            settings.DirectionsCosts[direction]));
                                }

                                break;
                            }
                            case Directions.Top:
                            {
                                if (map.TryGetValue(depth + 1, out var values) &&
                                    values.GetLength(0) > height &&
                                    values.GetLength(1) > width)
                                {
                                    if (height <= -1 || width <= -1) continue;
                                    if (values[height, width] == -1) continue;
                                    tempDic.Add(direction,
                                        new DirectionConfig(map[depth + 1][height, width],
                                            settings.DirectionsCosts[direction]));
                                }

                                break;
                            }
                            case Directions.Bottom:
                            {
                                if (map.TryGetValue(depth - 1, out var values) &&
                                    values.GetLength(0) > height &&
                                    values.GetLength(1) > width)
                                {
                                    if (height <= -1 || width <= -1) continue;
                                    if (values[height, width] == -1) continue;
                                    tempDic.Add(direction,
                                        new DirectionConfig(map[depth - 1][height, width],
                                            settings.DirectionsCosts[direction]));
                                }

                                break;
                            }
                        }
                    }

                    listOfPoints.Add(new PointConfig(map[depth][height, width], settings.PointCost, tempDic));
                }
            }
        }

        Dictionary<Guid, List<PointConfig>> result;
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
            result = new Dictionary<Guid, List<PointConfig>>
            {
                { Guid.NewGuid(), listOfPoints }
            };
        }

        return new MapConfigWithPoints(_mapId, result, settings.MapSize.Width, settings.MapSize.Height, settings.MapSize.Depth);
    }

    private static IDictionary<int, int[,]> InitializeMap(MapSettings settings, Random? random = null,
        bool intergalacticDummyMode = false)
    {
        var result = new Dictionary<int, int[,]>();
        for (var depth = 0; depth < settings.MapSize.Depth; depth++)
        {
            result.Add(depth, new int[settings.MapSize.Height, settings.MapSize.Width]);
        }

        for (var depth = 0; depth < settings.MapSize.Depth; depth++)
        {
            for (var height = 0; height < result[depth].GetLength(0); height++)
            {
                for (var width = 0; width < result[depth].GetLength(1); width++)
                {
                    if (intergalacticDummyMode)
                    {
                        result[depth][height, width] = 1;
                    }
                    else
                    {
                        result[depth][height, width] = random?.Next(0, 2) == 1 ? 1 : -1;
                    }
                }
            }
        }

        return result;
    }

    private static IDictionary<int, int[,]> ConvertToIndexBasedMap(IDictionary<int, int[,]> map)
    {
        var index = 1;
        for (var depth = 0; depth < map.Count; depth++)
        {
            for (var height = 0; height < map[depth].GetLength(0); height++)
            {
                for (var width = 0; width < map[depth].GetLength(1); width++)
                {
                    if (!map.TryGetValue(depth, out var ints)) continue;
                    
                    if (ints[height, width] > 0)
                    {
                        map[depth][height, width] = index++;
                    }
                    else
                    {
                        map[depth][height, width] = -1;
                    }
                }
            }
        }

        return map;
    }
}