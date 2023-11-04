namespace Akka.Pathfinder.Core.Messages;

public record PathFinderDone(Guid PathfinderId, Guid PathId, DateTimeOffset PathfinderStartTime, bool Success, string? ErrorMessage = default);
