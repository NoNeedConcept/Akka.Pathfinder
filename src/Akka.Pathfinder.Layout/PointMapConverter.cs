using Akka.Pathfinder.Core.Configs;

namespace Akka.Pathfinder.Layout;

[Flags]
public enum Level
{
    None = 0,
    One = 1,
    Two = 2,
}

public static class CostConstant
{
    public const int Default = 420;
}

public static class PointMapConverter
{
    public static MapConfig CreateMapConfig(IDictionary<Level, int[][]> input)
    {
        var result = new MapConfig(Guid.NewGuid(), new List<PointConfig>());
        foreach (var key in input.Keys)
        {
            for (long i = 0; i < input[key].LongLength; i++)
            {
                for (long j = 0; i < input[key][i].LongLength; i++)
                {
                    var valueOfMapItem = input[key][i][j];
                    var directionConfigs = new Dictionary<Direction, DirectionConfig>();
                    


                    result.Points.Add(new PointConfig( + 420, CostConstant.Default, directionConfigs));
                }
            }
        }




        return new MapConfig(Guid.NewGuid(), new());
    }
}
