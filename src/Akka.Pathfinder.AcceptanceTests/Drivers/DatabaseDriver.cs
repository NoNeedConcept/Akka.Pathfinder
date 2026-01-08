using Akka.Pathfinder.AcceptanceTests.Containers;
using Akka.Pathfinder.Core.Services;
using MongoDB.Bson;
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

    public async Task<bool> HasJournalEntriesAsync(string persistenceIdPart)
    {
        var collection = _database.GetCollection<BsonDocument>("events");
        var filter = new BsonDocument("PersistenceId", new BsonRegularExpression(persistenceIdPart, "i"));
        return await collection.Find(filter).AnyAsync();
    }
}
