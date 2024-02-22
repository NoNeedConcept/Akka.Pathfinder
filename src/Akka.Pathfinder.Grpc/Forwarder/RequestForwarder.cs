using System.Threading.Channels;
using Akka.Actor;
using Akka.Pathfinder.Core;
using Akka.Pathfinder.Core.Messages;
using Akka.Streams;
using Akka.Streams.Dsl;
using moin.akka.endpoint;
using Serilog;
using Endpoint = Akka.Pathfinder.Core.Endpoint;

namespace Akka.Pathfinder.Grpc.Forwarder;

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
        _logger.Information("[RequestForwarder][PreStart]");
        _queue = Source
            .Channel<RequestItem>(128)
            .Throttle(240, TimeSpan.FromMinutes(1), 120, x =>
            {
                if (x.Request is PathfinderRequest { Options.Timeout: not null } findPathRequest)
                {
                    return (int)findPathRequest.Options.Timeout.Value.TotalSeconds;
                }

                return 9;
            }, ThrottleMode.Shaping)
            .AskTransform(
                item => item.Request,
                new Func<IResponse, RequestItem, ResponseItem>(OutputItemCreator),
                item => GetActorRef(item.Request),
                8)
            .Select(response =>
            {
                var sender = response.Sender;
                if (sender.IsNobody()) return response;
                sender.Tell(response.Response, ActorRefs.NoSender);
                return response;
            })
            .Named("RequestQueue")
            .ToMaterialized(Sink.Ignore<ResponseItem>(), Keep.Left)
            .Run(Context.Materializer());
        return;

        ResponseItem OutputItemCreator(IResponse response, RequestItem item)
        {
            return new ResponseItem(item.Sender, response);
        }
    }

    protected override void PostStop()
    {
        _logger.Information("[RequestForwarder][PostStop]");
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