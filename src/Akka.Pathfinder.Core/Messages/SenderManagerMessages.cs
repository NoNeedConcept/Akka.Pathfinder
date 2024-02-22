namespace Akka.Pathfinder.Core.Messages;

public record SavePathfinderSender(Guid PathfinderId) : MessageBase;
public record ForwardToPathfinderSender(Guid PathfinderId, IResponse Message) : MessageBase;