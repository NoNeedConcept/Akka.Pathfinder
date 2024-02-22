using Akka.Pathfinder.Core.Configs;
using Akka.Pathfinder.Core.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using Servus.Core.Application.Startup;
using Path = Akka.Pathfinder.Core.Persistence.Data.Path;

namespace Akka.Pathfinder.Grpc.Startup;

public class ServiceSetupContainer : IServiceSetupContainer
{
    public void SetupServices(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("mongodb")!;
        services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        services.AddGrpc(options => { options.MaxReceiveMessageSize = null; });
        services
            .AddSingleton<IMongoClient>(_ => new MongoClient(connectionString))
            .AddTransient(x => x.GetRequiredService<IMongoClient>().GetDatabase("pathfinder"))
            .AddTransient(x => x.GetRequiredService<IMongoDatabase>().GetCollection<Path>("path"))
            .AddTransient(x => x.GetRequiredService<IMongoDatabase>().GetCollection<MapConfig>("map_config"))
            .AddTransient<IPathWriter, PathWriter>()
            .AddTransient<IPathReader, PathReader>()
            .AddTransient<IMapConfigWriter, MapConfigWriter>()
            .AddTransient<IMapConfigReader, MapConfigReader>()
            .AddTransient<IPointConfigReader, PointConfigReader>()
            .AddTransient<IPointConfigWriter, PointConfigWriter>();
    }
}