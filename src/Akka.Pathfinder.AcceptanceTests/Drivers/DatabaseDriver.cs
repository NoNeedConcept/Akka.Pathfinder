using Akka.Pathfinder.AcceptanceTests.Containers;
using Akka.Pathfinder.Core;
using MongoDB.Driver;
using Reqnroll.BoDi;
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

    public IPathReader CreatePathReader() => new PathReader(_database.GetCollection<Path>("path"));
}
