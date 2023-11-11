using Akka.Actor;
using Akka.Pathfinder.Core.Messages;

namespace Akka.Pathfinder.Managers;

public partial class SenderManager : ReceiveActor
{
    private readonly Serilog.ILogger _logger = Serilog.Log.Logger.ForContext<SenderManager>();
    private readonly Dictionary<Guid, IActorRef> _pathfinderSender = new();
    public SenderManager()
    {
        Receive<SavePathfinderSender>(SavePathfinderSenderHandler);
        Receive<ForwardToPathfinderSender>(ForwardToPathfinderSenderHandler);
    }
}
