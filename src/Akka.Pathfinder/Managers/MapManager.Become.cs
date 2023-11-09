using Akka.Pathfinder.Core.Messages;
using Akka.Persistence;

namespace Akka.Pathfinder.Managers;

public partial class MapManager : ReceivePersistentActor
{
    private void WaitingForPoints()
    {
        Command<IsMapReady>(IsMapReadyHandler);
        CommandAny(_ => {});
    }

    private void Ready()
    {
        CommandAsync<LoadMap>(LoadMapHandler);
        Command<UpdateMap>(UpdateMapHandler);
        Command<ResetMap>(ResetMapHandler);
        Command<IsMapReady>(IsMapReadyHandler);
        CommandAny(x => { });
    }
}
