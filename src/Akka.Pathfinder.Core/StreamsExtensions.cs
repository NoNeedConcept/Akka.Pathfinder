using Akka.Actor;
using Akka.Streams.Dsl;

namespace Akka.Pathfinder.Core;

public static class StreamsExtensions
{
    public static Source<TOutputItem, TMat> AskTransform<TInputItem, TRequest, TResponse, TOutputItem, TMat>(this Source<TInputItem, TMat> source, Func<TInputItem, TRequest> requestCreator, Func<TResponse, TInputItem, TOutputItem> outputItemCreator, Func<TInputItem, IActorRef> getActorRef, CancellationToken cancellationToken = default, int parallelism = 2)
    {
        var flow = Flow.Create<TInputItem>()
        .SelectAsync(parallelism, async item =>
        {
            var actorRef = getActorRef.Invoke(item);
            var request = requestCreator.Invoke(item);
            var response = await actorRef.Ask<TResponse>(request, cancellationToken);
            return outputItemCreator.Invoke(response, item);
        })
        .Named("AskTransform");

        return source.ViaMaterialized(flow, Keep.Left);
    }
}   
