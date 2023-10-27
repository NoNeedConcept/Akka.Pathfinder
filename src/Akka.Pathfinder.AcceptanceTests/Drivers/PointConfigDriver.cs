using Akka.Pathfinder.AcceptanceTests.Containers;
using Akka.Pathfinder.Core.Configs;
using MongoDB.Driver;

namespace Akka.Pathfinder.AcceptanceTests.Drivers;

public class PointConfigDriver
{
    private const string DatabaseName = "pathfinder";
    private readonly IMongoDatabase _mongoDatabase;
    
    public PointConfigDriver(MongoDbContainer mongoDbContainer)
    {
        _mongoDatabase = new MongoClient(mongoDbContainer.GetConnectionString()).GetDatabase(DatabaseName);
    }

    public async Task AddPointConfig(PointConfig pointConfig)
    {
        await _mongoDatabase.GetCollection<PointConfig>("point_config").InsertOneAsync(pointConfig);
    }
}