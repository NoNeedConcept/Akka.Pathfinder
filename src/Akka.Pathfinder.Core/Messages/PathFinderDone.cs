namespace Akka.Pathfinder.Core.Messages;

public record PathFinderDone(Guid PathfinderId, Guid PathId, bool Success, string? ErrorMessage = default);
