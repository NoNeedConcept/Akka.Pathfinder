using Akka.Pathfinder.Core.Messages;

namespace Akka.Pathfinder.Core;

public record SavePathfinderSender(Guid PathfinderId);

public record FowardToPathfinderSender(Guid PathfinderId, PathFinderDone Message);