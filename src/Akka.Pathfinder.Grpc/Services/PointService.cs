using Akka.Actor;
using Akka.Hosting;
using Akka.Pathfinder.Core;
using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Grpc.Conversions;
using Akka.Pathfinder.Grpc.Forwarder;
using Grpc.Core;

namespace Akka.Pathfinder.Grpc.Services;

public class PointService : Akka.Pathfinder.Grpc.PointService.PointServiceBase
{
    private readonly IActorRef _requestForwarder;
    private readonly Serilog.ILogger _logger;

    public PointService(IServiceProvider serviceProvider)
    {
        _logger = Serilog.Log.Logger.ForContext("SourceContext", GetType().Name);
        _requestForwarder = serviceProvider.GetRequiredService<IActorRegistry>().Get<RequestForwarder>();
    }

    private async Task<Ack> Execute<TResponse>(Func<IActorRef, CancellationToken, Task<TResponse>> action,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await action(_requestForwarder, cancellationToken);
            return response switch
            {
                UpdateCostResponse r => new Ack { Success = r.Success },
                PointCommandResponse r =>  new Ack { Success = r.Success },
                PointDeleted r => new Ack { Success = r.Success },
                _ => new Ack { Success = true }
            };
        }
        catch (RpcException ex) when (ex.StatusCode != StatusCode.Cancelled)
        {
            _logger.Error(ex, "[Error]][{ErrorMessage}]", ex.Message);
            return new Ack { Success = false };
        }
        catch (OperationCanceledException ex)
        {
            _logger.Error(ex, "[Canceled][{ErrorMessage}]", ex.Message);
            return new Ack { Success = false };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "[Error]][{ErrorMessage}]", ex.Message);
            return new Ack { Success = false };
        }
    }

    public override Task<Ack> Occupy(PointRequest request, ServerCallContext context)
        => Execute((g, c) => g.AskTraced<UpdateCostResponse>(request.ToOccupied(), context.CancellationToken), context.CancellationToken);

    public override Task<Ack> Release(PointRequest request, ServerCallContext context)
        => Execute((g, c) => g.AskTraced<UpdateCostResponse>(request.ToReleased(), context.CancellationToken), context.CancellationToken);

    public override Task<Ack> Block(PointRequest request, ServerCallContext context)
        => Execute((g, c) => g.AskTraced<PointCommandResponse>(request.ToBlock(), context.CancellationToken), context.CancellationToken);

    public override Task<Ack> Unblock(PointRequest request, ServerCallContext context)
        => Execute((g, c) => g.AskTraced<PointCommandResponse>(request.ToUnblock(), context.CancellationToken), context.CancellationToken);

    public override Task<Ack> UpdateDirection(PointConfig request, ServerCallContext context)
        => Execute((g, c) => g.AskTraced<PointDirectionUpdated>(request.ToUpdateDirection(), context.CancellationToken), context.CancellationToken);

    public override Task<Ack> IncreaseCost(UpdateCostRequest request, ServerCallContext context)
        => Execute((g, c) => g.AskTraced<UpdateCostResponse>(request.ToIncrease(), context.CancellationToken), context.CancellationToken);

    public override Task<Ack> DecreaseCost(UpdateCostRequest request, ServerCallContext context)
        => Execute((g, c) => g.AskTraced<UpdateCostResponse>(request.ToDecrease(), context.CancellationToken), context.CancellationToken);

    public override Task<Ack> IncreaseDirectionCost(UpdateDirectionCostRequest request, ServerCallContext context)
        => Execute((g, c) => g.AskTraced<UpdateCostResponse>(request.ToIncrease(), context.CancellationToken), context.CancellationToken);

    public override Task<Ack> DecreaseDirectionCost(UpdateDirectionCostRequest request, ServerCallContext context)
        => Execute((g, c) => g.AskTraced<UpdateCostResponse>(request.ToDecrease(), context.CancellationToken), context.CancellationToken);
}