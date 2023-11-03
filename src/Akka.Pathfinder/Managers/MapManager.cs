using Akka.Actor;
using Akka.Pathfinder.Core.Messages;

namespace Akka.Pathfinder.Managers;

public class MapManager : ReceiveActor
{
    public MapManager()
    {
        Receive<InitializeMap>(InitializeMap);
    }

    public void InitializeMap(InitializeMap msg)
    {
        
    }
}