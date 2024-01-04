using Akka.Pathfinder.Core.Messages;
using Akka.Persistence;

namespace Akka.Pathfinder.Managers;

public partial class MapManager : ReceivePersistentActor
{
    private void Ready()
    {   
        _logger.Information("[MapManager][READY]");
        CommandAsync<LoadMap>(LoadMapHandler);
        CommandAsync<UpdateMap>(UpdateMapHandler);
        CommandAsync<ResetMap>(ResetMapHandler);
        Command<FindPathRequest>(FindPathRequestHandler);
        CommandAny(msg => Stash.Stash());
        Stash.UnstashAll();
    }

    private void Waiting() => CommandAny(msg => Stash.Stash());
}
