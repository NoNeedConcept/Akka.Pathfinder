namespace Akka.Pathfinder.Core.Configs;

public record PointConfig(int Id, uint Cost, Dictionary<Directions, DirectionConfig> DirectionConfigs, bool HasChanges = false);

public record DirectionConfig(int TargetPointId, uint Cost)
{
    internal DirectionConfig Increase(uint value) => this with { Cost = Cost + value };
    internal DirectionConfig Decrease(uint value) => this with { Cost = Cost - value };
}

[Flags]
public enum Directions : byte
{
    None = 0,
    Top = 1 << 0,
    Bottom = 1 << 1,
    Left = 1 << 2,
    Right = 1 << 3,
    Front = 1 << 4,
    Back = 1 << 5,
    FrontBack = Front | Back,
    LeftRightTopBottom = Left | Right | Top | Bottom,
    All = Top | Bottom | Left | Right | Front | Back
}