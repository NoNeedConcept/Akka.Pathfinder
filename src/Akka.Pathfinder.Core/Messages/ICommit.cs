using Akka.Pathfinder.Core.Configs;

namespace Akka.Pathfinder.Core.Messages;

public interface ICommit
{
    internal ChangeMethod ChangeMethod { get; }
    uint AdditionalCost { get; }
}

public class DirectionCommit : ICommit
{
    private readonly ChangeMethod _changeMethod;

    public DirectionCommit(uint additionalCost, Direction direction, ChangeMethod change)
    {
        Direction = direction;
        AdditionalCost = additionalCost;
        _changeMethod = change;
    }

    public Direction Direction { get; init; } = Direction.None;

    public uint AdditionalCost { get; init; }

    ChangeMethod ICommit.ChangeMethod => _changeMethod;
}

public class PointCommit : ICommit
{
    private readonly ChangeMethod _changeMethod;

    public PointCommit(uint additionalCost, ChangeMethod change)
    {
        AdditionalCost = additionalCost;
        _changeMethod = change;
    }

    public uint AdditionalCost { get; init; }

    ChangeMethod ICommit.ChangeMethod => _changeMethod;
}

public enum ChangeMethod
{
    Invalid,
    Increase,
    Decrease,
}