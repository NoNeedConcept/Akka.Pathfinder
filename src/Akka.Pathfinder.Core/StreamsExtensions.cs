using Akka.Actor;
using Akka.Streams.Dsl;
using Servus.Core.Diagnostics;

namespace Akka.Pathfinder.Core;

public static class StreamsExtensions
{
    public static Source<TOutputItem, TMat> AskTransform<TInputItem, TRequest, TResponse, TOutputItem, TMat>(
        this Source<TInputItem, TMat> source, Func<TInputItem, TRequest> requestCreator,
        Func<TResponse, TInputItem, TOutputItem> outputItemCreator, Func<TInputItem, IActorRef> getActorRef,
        int parallelism = 2)
    {
        var flow = Flow.Create<TInputItem>()
            .SelectAsync(parallelism, async item =>
            {
                var actorRef = getActorRef.Invoke(item);
                var request = requestCreator.Invoke(item);
                var response = await actorRef.Ask<TResponse>(request);
                return outputItemCreator.Invoke(response, item);
            })
            .Named("AskTransform");

        return source.ViaMaterialized(flow, Keep.Left);
    }

    public static Source<TOut2, TMat> AskTraced<TOut, TOut2, TMat>(this Source<TOut, TMat> source, IActorRef actorRef,
        int parallelism = 2, CancellationToken cancellationToken = default) where TOut : IWithTracing
    {
        var flow = Flow.Create<TOut>()
            .SelectAsync(parallelism, async e =>
            {
                var reply = await actorRef.AskTraced<TOut2>(e, cancellationToken);
                return reply switch
                {
                    { } a => a,
                    _ => throw new InvalidOperationException(
                        $"Expected to receive response of type {nameof(TOut2)}, but got: {reply}")
                };
            })
            .Named("AskTraced");

        return source.ViaMaterialized(flow, Keep.Left);
    }
}