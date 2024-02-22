using Akka.Actor;
using Servus.Akka.Messaging;
using Servus.Core.Diagnostics;

namespace Akka.Pathfinder.Core;

public static class ActorRefExtensions
{
    public static async Task<T> AskTraced<T>(this IActorRef recipient, object message, TimeSpan? timeout = null)
        => await recipient.AskTraced<T>(message, timeout, CancellationToken.None);

    public static async Task<T> AskTraced<T>(this IActorRef recipient, object message, CancellationToken cancellationToken)
        => await recipient.AskTraced<T>(message, null, cancellationToken);

    public static async Task<T> AskTraced<T>(this IActorRef recipient, object message, TimeSpan? timeout,
        CancellationToken cancellationToken)
        => await recipient.AskTraced<T>(new TracedMessageEnvelope(message), timeout, cancellationToken);

    public static async Task<T> AskTraced<T>(this IActorRef recipient, IWithTracing message, TimeSpan? timeout = null)
        => await recipient.AskTraced<T>(message, timeout, CancellationToken.None);

    public static async Task<T> AskTraced<T>(this IActorRef recipient, IWithTracing message, CancellationToken cancellationToken)
        => await recipient.AskTraced<T>(message, null, cancellationToken);

    public static async Task<T> AskTraced<T>(this IActorRef recipient, IWithTracing message, TimeSpan? timeout, CancellationToken cancellationToken)
    {
        message.AddTracing();
        return await recipient.Ask<T>(message, timeout, cancellationToken);
    }

    public static T WithTracing<T>(this T tracing, string? traceId, string? spanId) where T : IWithTracing
    {
        tracing.AddTracing(traceId, spanId);
        return tracing;
    }
    
    public static T WithTracing<T>(this T tracing, IWithTracing value) where T : IWithTracing
    {
        tracing.AddTracing(value);
        return tracing;
    }
    
    public static T WithTracing<T>(this T tracing) where T : IWithTracing
    {
        tracing.AddTracing();
        return tracing;
    }
}