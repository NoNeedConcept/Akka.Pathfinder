namespace Akka.Pathfinder.Core.Messages;

public interface IPointId : IEntityId
{
    int PointId { get; }
    string IEntityId.EntityId => PointId.ToString();
}
