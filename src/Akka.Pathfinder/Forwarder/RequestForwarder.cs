using System.Threading.Channels;
using Akka.Actor;
using Akka.Pathfinder.Core;
using Akka.Streams;
using Akka.Streams.Dsl;
using moin.akka.endpoint;
using Serilog;

namespace Akka.Pathfinder;

public class RequestForwarder : ReceiveActor
{
    private readonly Serilog.ILogger _logger;
    private ChannelWriter<RequestItem> _queue = null!;

    public RequestForwarder()
    {
        _logger = Log.Logger.ForContext("SourceContext", GetType().Name);
        ReceiveAsync<IRequest>(async msg => await _queue.WriteAsync(new RequestItem(Sender, msg)));
    }

    protected override void PreStart()
    {
        _logger.Verbose("[RequestForwarder][PreStart]");
        _queue = Source
                .Channel<RequestItem>(256)
                .AskTransform(item => item.Request, new Func<IResponse, RequestItem, ResponseItem>((response, item) => new ResponseItem(item.Sender, response)), item => GetActorRef(item.Request), parallelism: 128)
                .Select(response =>
                {
                    var sender = response.Sender;
                    if (sender.IsNobody()) return response;
                    sender.Tell(response.Response, ActorRefs.NoSender);
                    return response;
                })
                .ToMaterialized(Sink.Ignore<ResponseItem>(), Keep.Left)
                .Named("RequestQueue")
                .Run(Context.Materializer());
    }

    protected override void PostStop()
    {
        _logger.Verbose("[RequestForwarder][PostStop]");
        _queue.Complete();
    }

    private static IActorRef GetActorRef(IRequest request)
        => request switch
        {
            IMapManagerRequest => Context.GetRegistry().GetClient<Endpoint.MapManager>(),
            IPathfinderRequest => Context.GetRegistry().GetClient<Endpoint.PathfinderWorker>(),
            IPointRequest => Context.GetRegistry().GetClient<Endpoint.PointWorker>(),
            _ => ActorRefs.Nobody
        };
}

internal record RequestItem(IActorRef Sender, IRequest Request, CancellationToken CancellationToken = default);
internal record ResponseItem(IActorRef Sender, IResponse Response, CancellationToken CancellationToken = default);