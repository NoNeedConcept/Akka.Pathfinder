namespace Akka.Pathfinder.Core.Messages;

public interface IPathfinderId : IEntityId
{
    Guid PathfinderId { get; }
    string IEntityId.EntityId => PathfinderId.ToString();
}