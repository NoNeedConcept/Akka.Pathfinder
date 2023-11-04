using Path = Akka.Pathfinder.Core.Persistence.Data.Path;

namespace Akka.Pathfinder.Core.Messages;

public record PathFinderDone(Path? Path);
