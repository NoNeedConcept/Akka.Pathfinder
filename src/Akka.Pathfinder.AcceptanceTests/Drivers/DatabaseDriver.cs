﻿using Akka.Pathfinder.AcceptanceTests.Containers;
using Akka.Pathfinder.Core.Services;
using MongoDB.Bson;
using MongoDB.Driver;
using Reqnroll.BoDi;
using Serilog;
using Path = Akka.Pathfinder.Core.Persistence.Data.Path;

namespace Akka.Pathfinder.AcceptanceTests;

public class DatabaseDriver
{
    private readonly IMongoDatabase _database;
    private readonly ILogger _logger = Log.Logger.ForContext<DatabaseDriver>();
    
    public DatabaseDriver(IObjectContainer container)
    {
        try
        {
            var mongoContainer = container.Resolve<MongoDbContainer>();
            var client = mongoContainer.CreateMongoClient();
            _database = client.GetDatabase("pathfinder");
            _logger.Information("[TEST][DatabaseDriver] Initialized with database: pathfinder");
        }
        catch (Exception ex)
        {
            _logger.Error("[TEST][DatabaseDriver] Error initializing: {Exception}", ex);
            throw;
        }
    }

    public IPathReader CreatePathReader() 
    {
        try
        {
            var collection = _database.GetCollection<Path>("path");
            return new PathReader(collection);
        }
        catch (Exception ex)
        {
            _logger.Error("[TEST][DatabaseDriver] Error creating PathReader: {Exception}", ex);
            throw;
        }
    }

    public async Task<bool> HasJournalEntriesAsync(string persistenceIdPart)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var collection = _database.GetCollection<BsonDocument>("events");
            var filter = new BsonDocument("PersistenceId", new BsonRegularExpression(persistenceIdPart, "i"));
            var result = await collection.Find(filter).AnyAsync(cts.Token);
            _logger.Information("[TEST][DatabaseDriver] Journal entry check: {PersistenceIdPart} - {Result}", 
                persistenceIdPart, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error("[TEST][DatabaseDriver] Error checking journal entries for {PersistenceIdPart}: {Exception}", 
                persistenceIdPart, ex);
            throw;
        }
    }
}
