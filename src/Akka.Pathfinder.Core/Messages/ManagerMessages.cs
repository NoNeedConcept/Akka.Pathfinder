namespace Akka.Pathfinder.Core.Messages;

public record LoadMap(Guid MapId);
public record UpdateMap(Guid MapId);
public record ResetMap(Guid MapId);

public record MapLoaded(Guid MapId);
public record MapUpdated(Guid MapId);