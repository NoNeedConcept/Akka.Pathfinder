using Akka.Actor;
using Akka.Pathfinder.Core;

namespace Akka.Pathfinder.Managers;

public partial class SenderManager
{
    private void SavePathfinderSenderHandler(SavePathfinderSender msg)
    {
        _logger.Debug("[SenderManager][{MessageType}] received", msg.GetType().Name);
        _pathfinderSender.Add(msg.PathfinderId, Sender);
    }

    private void FowardToPathfinderSenderHandler(FowardToPathfinderSender msg)
    {
        _logger.Debug("[SenderManager][{MessageType}] received", msg.GetType().Name);
        if(_pathfinderSender.TryGetValue(msg.PathfinderId, out var sender))
        {
            sender.Tell(msg.Message);
        }
    }
}
