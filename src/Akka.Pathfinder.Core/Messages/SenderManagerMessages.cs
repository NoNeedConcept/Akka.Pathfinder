namespace Akka.Pathfinder.Core.Messages;

public record SavePathfinderSender(Guid PathfinderId);
public record ForwardToPathfinderSender(Guid PathfinderId, PathFinderDone Message);