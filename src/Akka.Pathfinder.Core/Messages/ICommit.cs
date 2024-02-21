using Akka.Pathfinder.Core.Configs;

namespace Akka.Pathfinder.Core.Messages;

public interface ICommit
{
    internal ChangeMethod ChangeMethod { get; }
    uint AdditionalCost { get; }
}

public class DirectionCommit(uint additionalCost, Direction direction, ChangeMethod change) : ICommit
{
    private readonly ChangeMethod _changeMethod = change;

    public Direction Direction { get; init; } = direction;

    public uint AdditionalCost { get; init; } = additionalCost;

    ChangeMethod ICommit.ChangeMethod => _changeMethod;
}

public class PointCommit(uint additionalCost, ChangeMethod change) : ICommit
{
    private readonly ChangeMethod _changeMethod = change;

    public uint AdditionalCost { get; init; } = additionalCost;

    ChangeMethod ICommit.ChangeMethod => _changeMethod;
}

public enum ChangeMethod
{
    Invalid,
    Increase,
    Decrease,
}