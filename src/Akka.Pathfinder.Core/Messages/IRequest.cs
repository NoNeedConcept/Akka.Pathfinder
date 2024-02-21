
namespace Akka.Pathfinder.Core;

public interface IMessage
{
    Guid MessageId { get; init; }
}

public abstract record MessageBase() : IMessage
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
}

public interface IRequest : IMessage
{
    Guid RequestId { get; init; }
    Type ResponseType { get; }
}

public interface IRequest<TResponse> : IRequest where TResponse : IResponse;

public interface IPointRequest : IRequest, IPointId;
public interface IPointRequest<TResponse> : IRequest<TResponse>, IPointRequest where TResponse : IResponse;

public interface IPathfinderRequest : IRequest, IPathfinderId;
public interface IPathfinderRequest<TResponse> : IRequest<TResponse>, IPathfinderRequest where TResponse : IResponse;

public interface IMapManagerRequest : IRequest; 
public interface IMapManagerRequest<TResponse> : IRequest<TResponse>, IMapManagerRequest where TResponse : IResponse;

public abstract record RequestBase<TResponse>(Guid RequestId) : MessageBase(), IRequest<TResponse> where TResponse : IResponse
{
    public Type ResponseType => typeof(TResponse);
}

public interface IResponse : IMessage
{
    Guid RequestId { get; init; }
}

public abstract record ResponseBase(Guid RequestId) : MessageBase(), IResponse
{ }


// Sharding entities
public interface IPathfinderId : IEntityId
{
    Guid PathfinderId { get; }
    string IEntityId.EntityId => PathfinderId.ToString();
}

public interface IPointId : IEntityId
{
    int PointId { get; }
    string IEntityId.EntityId => PointId.ToString();
}