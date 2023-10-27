using Akka.Pathfinder.Core.Configs;
using Akka.Pathfinder.Core.Messages;

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

    public static bool ChangePointCost(this PointWorkerState state, uint value, ChangeMethod changeMethod) => state.ApplyCommit(new PointCommit(value, changeMethod));

    public static bool ChangeDirectionCost(this PointWorkerState state, uint value, Direction direction, ChangeMethod changeMethod) => state.ApplyCommit(new DirectionCommit(value, direction, changeMethod));
}
