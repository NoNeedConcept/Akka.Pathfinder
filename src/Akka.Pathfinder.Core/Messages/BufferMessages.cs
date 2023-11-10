namespace Akka.Pathfinder.Core;


public record InitializeBuffer(int PointId, Guid MapId);

public record PathfinderHasPointsArrived(Guid PathfinderId, int PointId, int[] NextPoints) : IBufferId;

