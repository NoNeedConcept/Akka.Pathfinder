using Akka.Actor;
using Akka.Hosting;
using Akka.Pathfinder.Core.Messages;
using Akka.Pathfinder.Grpc;
using Grpc.Core;
using moin.akka.endpoint;
using Servus.Akka.Diagnostics;

namespace Akka.Pathfinder;

public class PointService : Grpc.PointService.PointServiceBase
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly Serilog.ILogger _logger;

    public PointService(IServiceScopeFactory scopeFactory)
    {
        _logger = Serilog.Log.Logger.ForContext("SourceContext", GetType().Name);
        _serviceScopeFactory = scopeFactory;
    }

    private async Task<Ack> Execute<TResponse>(Func<IActorRef, CancellationToken, Task<TResponse>> action,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var pointWorkerClient = scope.ServiceProvider.GetRequiredService<IActorRegistry>().GetClient<Endpoint.PointWorker>();
            var response = await action(pointWorkerClient, cancellationToken);
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
        => Execute((g, c) => g.AskTraced<UpdateCostResponse>(request.ToOccupied()), context.CancellationToken);

    public override Task<Ack> Release(PointRequest request, ServerCallContext context)
        => Execute((g, c) => g.AskTraced<UpdateCostResponse>(request.ToReleased()), context.CancellationToken);

    public override Task<Ack> Block(PointRequest request, ServerCallContext context)
        => Execute((g, c) => g.AskTraced<PointCommandResponse>(request.ToBlock()), context.CancellationToken);

    public override Task<Ack> Unblock(PointRequest request, ServerCallContext context)
        => Execute((g, c) => g.AskTraced<PointCommandResponse>(request.ToUnblock()), context.CancellationToken);

    public override Task<Ack> UpdateDirection(PointConfig request, ServerCallContext context)
        => Execute((g, c) => g.AskTraced<PointDirectionUpdated>(request.ToUpdateDirection()), context.CancellationToken);

    public override Task<Ack> IncreaseCost(UpdateCostRequest request, ServerCallContext context)
        => Execute((g, c) => g.AskTraced<UpdateCostResponse>(request.ToIncrease()), context.CancellationToken);

    public override Task<Ack> DecreaseCost(UpdateCostRequest request, ServerCallContext context)
        => Execute((g, c) => g.AskTraced<UpdateCostResponse>(request.ToDecrease()), context.CancellationToken);

    public override Task<Ack> IncreaseDirectionCost(UpdateDirectionCostRequest request, ServerCallContext context)
        => Execute((g, c) => g.AskTraced<UpdateCostResponse>(request.ToIncrease()), context.CancellationToken);

    public override Task<Ack> DecreaseDirectionCost(UpdateDirectionCostRequest request, ServerCallContext context)
        => Execute((g, c) => g.AskTraced<UpdateCostResponse>(request.ToDecrease()), context.CancellationToken);
}