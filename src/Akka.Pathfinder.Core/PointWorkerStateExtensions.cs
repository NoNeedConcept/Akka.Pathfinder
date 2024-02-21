using Akka.Pathfinder.Core.Configs;
using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Core.States;

namespace Akka.Pathfinder.Core;

public static class PointStateExtensions
{
    internal static bool ApplyCommit(this PointWorkerState state, ICommit commit)
    {
        return commit switch
        {
            PointCommit value => state.UpdatePointCost(value),
            DirectionCommit value => state.UpdateDirectionCost(value),
            _ => false
        };
    }

    public static bool ChangePointCost(this PointWorkerState state, uint value, ChangeMethod changeMethod)
        => state.ApplyCommit(new PointCommit(value, changeMethod));

    public static bool ChangeDirectionCost(this PointWorkerState state, uint value, Direction direction, ChangeMethod changeMethod)
        => state.ApplyCommit(new DirectionCommit(value, direction, changeMethod));

    public static Dictionary<Direction, DirectionConfig> MergeDirectionConfigs(this PointWorkerState state, IDictionary<Direction, DirectionConfig> configs)
    {
        var newDirectionConfigs = configs.ExceptBy(state.DirectionConfigs.Keys, x => x.Key).ToDictionary(x => x.Key, x => x.Value);
        return state.DirectionConfigs.Union(newDirectionConfigs).ToDictionary(x => x.Key, x => x.Value);
    }
}
