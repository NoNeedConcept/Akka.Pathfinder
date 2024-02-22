using Akka.Pathfinder.Core.Configs;

namespace Akka.Pathfinder.Core.Messages;

public interface ICommit
{
    internal ChangeMethod ChangeMethod { get; }
    uint AdditionalCost { get; }
}

public class DirectionCommit(uint additionalCost, Directions direction, ChangeMethod change) : ICommit
{
    public Directions Direction { get; init; } = direction;

    public uint AdditionalCost { get; init; } = additionalCost;

    ChangeMethod ICommit.ChangeMethod { get; } = change;
}

public class PointCommit(uint additionalCost, ChangeMethod change) : ICommit
{
    public uint AdditionalCost { get; init; } = additionalCost;

    ChangeMethod ICommit.ChangeMethod { get; } = change;
}

public enum ChangeMethod
{
    Invalid,
    Increase,
    Decrease,
}