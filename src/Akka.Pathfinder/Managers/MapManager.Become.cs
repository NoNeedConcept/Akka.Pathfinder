using Akka.Pathfinder.Core.Messages;

namespace Akka.Pathfinder.Managers;

public partial class MapManager
{
    private void Ready()
    {   
        _logger.Information("[MapManager][READY]");
        CommandAsync<LoadMap>(LoadMapHandler);
        Command<UpdateMap>(UpdateMapHandler);
        Command<FindPathRequest>(FindPathRequestHandler);
        CommandAny(msg => Stash.Stash());
        Stash.UnstashAll();
    }

    private void Waiting() => CommandAny(msg => Stash.Stash());
}
