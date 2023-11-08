using Akka.Pathfinder.Core.Messages;
using Akka.Persistence;

namespace Akka.Pathfinder.Managers;

public partial class MapManager : ReceivePersistentActor
{
    private void WaitingForPoints()
    {
        Command<PointInitialized>(PointInitializedHandler);
        Command<IsMapReady>(IsMapReadyHandler);
        Command<AllPointsInitialized>(AllPointsInitializedHandler);
        Command<NotAllPointsInitialized>(NotAllPointsInitializedHandler);
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
