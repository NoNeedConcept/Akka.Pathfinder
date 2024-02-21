using Akka.Pathfinder.AcceptanceTests.Containers;
using Akka.Pathfinder.Core;
using Akka.Pathfinder.Core.Configs;
using BoDi;
using MongoDB.Driver;
using Path = Akka.Pathfinder.Core.Persistence.Data.Path;

namespace Akka.Pathfinder.AcceptanceTests;

public class DatabaseDriver
{
    private readonly IMongoDatabase _database;
    public DatabaseDriver(IObjectContainer container)
    {
        var mongoContainer = container.Resolve<MongoDbContainer>();
        var client = mongoContainer.CreateMongoClient();
        _database = client.GetDatabase("pathfinder");
    }

    public IPointConfigWriter CreatePointConfigWriter() => new PointConfigWriter(_database);

    public IMapConfigWriter CreateMapConfigWriter() => new MapConfigWriter(_database.GetCollection<MapConfig>("map_config"));

    public IPathWriter CreatePathWriter() => new PathWriter(_database.GetCollection<Path>("path"));
}
