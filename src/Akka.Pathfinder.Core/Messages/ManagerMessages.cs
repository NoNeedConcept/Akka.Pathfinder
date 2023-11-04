namespace Akka.Pathfinder.Core.Messages;

public record LoadMap(Guid MapId);
public record UpdateMap(Guid MapId);
public record ResetMap(Guid MapId);

public record IsMapReady(Guid PathFinderId);

public abstract record AllPoints();

public record AllPointsInitialized() : AllPoints;

public record NotAllPointsInitialized() : AllPoints;

public record PointInitialized(int PointId);