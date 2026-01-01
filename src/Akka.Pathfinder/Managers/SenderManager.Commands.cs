using Akka.Pathfinder.Core.Messages;
using Servus.Akka.Diagnostics;

namespace Akka.Pathfinder.Managers;

public partial class SenderManager
{
    private void SavePathfinderSenderHandler(SavePathfinderSender msg)
    {
        using var activity = Telemetry.ActivitySource.StartActivity(msg.GetType().Name);
        _logger.Verbose("[SenderManager][{MessageType}] received", msg.GetType().Name);
        _pathfinderSender.Add(msg.PathfinderId, Sender);
    }

    private void ForwardToPathfinderSenderHandler(ForwardToPathfinderSender msg)
    {
        using var activity = Telemetry.ActivitySource.StartActivity(msg.GetType().Name);
        _logger.Verbose("[SenderManager][{MessageType}] received", msg.GetType().Name);
        if (_pathfinderSender.TryGetValue(msg.PathfinderId, out var sender))
        {
            sender.ForwardTraced(msg.Message);
            _pathfinderSender.Remove(msg.PathfinderId);
        }
    }
}