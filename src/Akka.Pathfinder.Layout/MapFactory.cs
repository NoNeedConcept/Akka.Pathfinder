using System.Drawing;
using Akka.DistributedData;
using Akka.Pathfinder.Core.Configs;

namespace Akka.Pathfinder.Layout;

public record MapSize(int Width, int Height, int Depth);

public static class MapFactory
{
    private static int[,,] Map { get; set; }
    private static MapSize MapSize { get; set; }
    private static Random Random { get; set; }

    private const int Emptypoint = -1;
    private const int DefaultPointCost = 42;
    private const int DefaultDirectionCost = 42;

    public static MapConfig Create(int seed, MapSize mapSize, bool intergalacticDummyMode = false)
    {
        Random = new Random(seed);
        MapSize = mapSize;

        InitializeMap(intergalacticDummyMode);
        return ConvertToMapConfig();
    }
    
    

    private static MapConfig ConvertToMapConfig()
    {
        var listOfPoints = new List<PointConfig>();
        int widthCounter = 0, heightCounter = 0, depthCounter = 0;
        int index = 0;
        while (widthCounter < MapSize.Width)
        {
            while (heightCounter < MapSize.Height)
            {
                while (depthCounter < MapSize.Depth)
                {
                    index++;
                    if (Map[widthCounter, heightCounter, depthCounter] == Emptypoint)
                    {
                        depthCounter++;
                        continue;
                    }

                    List<Direction> directionsToCheck = new List<Direction>();

                    if (widthCounter > 0) directionsToCheck.Add(Direction.Left);
                    if (heightCounter > 0) directionsToCheck.Add(Direction.Top);
                    if (depthCounter > 0) directionsToCheck.Add(Direction.Back);

                    if (widthCounter < MapSize.Width-1) directionsToCheck.Add(Direction.Right);
                    if (heightCounter < MapSize.Height-1) directionsToCheck.Add(Direction.Bottom);
                    if (depthCounter < MapSize.Depth-1) directionsToCheck.Add(Direction.Front);


                    var tempDic = new Dictionary<Direction, DirectionConfig>();

                    foreach (Direction direction in directionsToCheck)
                    {
                        switch (direction)
                        {
                            case Direction.Top:
                            {
                                if (Map[widthCounter, heightCounter - 1, depthCounter] != Emptypoint)
                                {
                                    tempDic.Add(direction,
                                        new DirectionConfig(Map[widthCounter, heightCounter - 1, depthCounter],
                                            DefaultDirectionCost));
                                }

                                break;
                            }
                            case Direction.Bottom:
                            {
                                if (Map[widthCounter, heightCounter + 1, depthCounter] != Emptypoint)
                                {
                                    tempDic.Add(direction,
                                        new DirectionConfig(Map[widthCounter, heightCounter + 1, depthCounter],
                                            DefaultDirectionCost));
                                }

                                break;
                            }
                            case Direction.Left:
                            {
                                if (Map[widthCounter - 1, heightCounter, depthCounter] != Emptypoint)
                                {
                                    tempDic.Add(direction,
                                        new DirectionConfig(Map[widthCounter - 1, heightCounter, depthCounter],
                                            DefaultDirectionCost));
                                }

                                break;
                            }
                            case Direction.Right:
                            {
                                if (Map[widthCounter + 1, heightCounter, depthCounter] != Emptypoint)
                                {
                                    tempDic.Add(direction,
                                        new DirectionConfig(Map[widthCounter + 1, heightCounter, depthCounter],
                                            DefaultDirectionCost));
                                }

                                break;
                            }
                            case Direction.Front:
                            {
                                if (Map[widthCounter, heightCounter, depthCounter + 1] != Emptypoint)
                                {
                                    tempDic.Add(direction,
                                        new DirectionConfig(Map[widthCounter, heightCounter, depthCounter + 1],
                                            DefaultDirectionCost));
                                }

                                break;
                            }
                            case Direction.Back:
                            {
                                if (Map[widthCounter, heightCounter, depthCounter - 1] != Emptypoint)
                                {
                                    tempDic.Add(direction,
                                        new DirectionConfig(Map[widthCounter, heightCounter, depthCounter - 1],
                                            DefaultDirectionCost));
                                }

                                break;
                            }
                        }
                    }

                    listOfPoints.Add(new PointConfig(index, DefaultPointCost, tempDic));

                    depthCounter++;
                }

                depthCounter = 0;
                heightCounter++;
            }


            heightCounter = 0;
            widthCounter++;
        }

        return new MapConfig(Guid.NewGuid(), listOfPoints);
    }

    private static void InitializeMap(bool intergalacticDummyMode)
    {
        Map = new int[MapSize.Width, MapSize.Depth, MapSize.Height];
        int widthCounter = 0, heightCounter = 0, depthCounter = 0;
        int index = 0;
        while (widthCounter < MapSize.Width)
        {
            while (heightCounter < MapSize.Height)
            {
                while (depthCounter < MapSize.Depth)
                {
                    index++;
                    if (intergalacticDummyMode)
                    {
                        Map[widthCounter, heightCounter, depthCounter] = index;
                    }
                    else
                    {
                        Map[widthCounter, heightCounter, depthCounter] = Random.Next(0, 2) == 1 ? index : Emptypoint;
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