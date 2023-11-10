namespace Akka.Pathfinder.Core;

public interface IBufferId : IEntityId
{
    int PointId { get; }
    string IEntityId.EntityId => Evaluator.GetBufferWorkerId(PointId);
}

public static class Evaluator
{
    public static string GetBufferWorkerId(int pointId) => (pointId / BufferConstant.Divider + 1).ToString();
}