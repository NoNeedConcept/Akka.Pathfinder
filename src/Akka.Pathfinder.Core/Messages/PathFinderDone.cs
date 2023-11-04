namespace Akka.Pathfinder.Core.Messages;

public record PathFinderDone(Guid PathId, bool Success, string? ErrorMessage = default);
