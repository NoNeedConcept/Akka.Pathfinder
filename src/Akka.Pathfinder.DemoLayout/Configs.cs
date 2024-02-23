namespace Akka.Pathfinder.DemoLayout;

public record MapConfig(Guid Id, IReadOnlyCollection<Guid> CollectionIds, int Count)
{ }

public record MapConfigWithPoints(Guid Id, IDictionary<Guid, List<PointConfig>> Configs) : MapConfig(Id, Configs.Keys.ToList(), Configs.Values.SelectMany(x => x).Count());

public record PointConfig(int Id, uint Cost, Dictionary<Direction, DirectionConfig> DirectionConfigs, bool HasChanges = false);

public record DirectionConfig(int TargetPointId, uint Cost);

[Flags]
public enum Direction : byte
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