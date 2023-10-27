using DotNet.Testcontainers.Builders;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;
using IContainer = DotNet.Testcontainers.Containers.IContainer;

namespace Akka.Pathfinder.AcceptanceTests.Containers;

public class MongoDbContainer : IAsyncLifetime
{
    private const int InternalPort = 27017;
    private const string MongoDatabase = "pathfinder";
    private const string MongoUser = "mongo";
    private const string MongoPassword = "mongo";

    public MongoDbContainer()
    {
        Log.Information("[TEST][{MongoDbContainerName}] ctor", GetType().Name);
        Container = new ContainerBuilder()
            .WithName($"mongodb_{Guid.NewGuid():D}")
            .WithImage("mongo:latest")
            .WithPortBinding(InternalPort, true)
            .WithEnvironment("MONGO_INITDB_ROOT_USERNAME", $"{MongoUser}")
            .WithEnvironment("MONGO_INITDB_ROOT_PASSWORD", $"{MongoPassword}")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .AddCustomWaitStrategy(new WaitUntilLogMessage("Waiting for connections")))
            .Build();
    }

    public int Port { get; private set; }

    public string Hostname { get; private set; } = string.Empty;

    public IContainer Container { get; init; }

    public string GetConnectionString()
    {
        return $"mongodb://{MongoUser}:{MongoPassword}@{Hostname}:{Port}";
    }

    public async Task DropDataAsync()
    {
        Log.Information("[TEST][{MongoDbContainerName}] DropDataAsync", GetType().Name);

        var connectionString = $"mongodb://{MongoUser}:{MongoPassword}@{Hostname}:{Port}";
        var mongoClient = new MongoClient(connectionString);

        await mongoClient.DropDatabaseAsync(MongoDatabase);
        Log.Information("[TEST][{MongoDbContainerName}] DropDataAsync finished", GetType().Name);
    }

    public async Task InitializeAsync()
    {
        Log.Information("[TEST][{MongoDbContainerName}] InitializeAsync", GetType().Name);

        var timeoutCts = new CancellationTokenSource();
        timeoutCts.CancelAfter(TimeSpan.FromMinutes(1));

        await Container.StartAsync(timeoutCts.Token).ConfigureAwait(false);

        Port = Container.GetMappedPublicPort(InternalPort);
        Hostname = Container.Hostname;
        CreateMongoDatabaseUser();

        Log.Information("[TEST][{MongoDbContainerName}] started and ready on Port [{Hostname}:{PublicPort}]", GetType().Name, Hostname, Port);

        Log.Information("[TEST][{MongoDbContainerName}] InitializeAsync finished", GetType().Name);
    }

    public async Task DisposeAsync()
    {
        Log.Information("[TEST][{MongoDbContainerName}] DisposeAsync", GetType().Name);
        await Container.StopAsync();
        await Container.DisposeAsync();
        Log.Information("[TEST][{MongoDbContainerName}] DisposeAsync finished", GetType().Name);
    }

    private void CreateMongoDatabaseUser()
    {
        Log.Information("[TEST][{MongoDbContainerName}] Create mongodb user", GetType().Name);
        var mongoDatabase = GetMongoDatabase();
        var createUserCommand = new BsonDocument
        {
            { "createUser", MongoUser },
            { "pwd", MongoPassword },
            {
                "roles",
                new BsonArray
                {
                    new BsonDocument
                    {
                        { "db", MongoDatabase },
                        { "role", "readWrite" }
                    }
                }
            }
        };
        mongoDatabase.RunCommand<BsonDocument>(createUserCommand);
        Log.Information("[TEST][{MongoDbContainerName}] Created mongodb user", GetType().Name);
    }

    public IMongoClient CreateMongoClient()
    {
        var connectionString = $"mongodb://{MongoUser}:{MongoPassword}@{Hostname}:{Port}";
        Log.Debug("[TEST][{MongoDbContainerName}] MongoDB ConnectionString: {connectionString}", GetType().Name, connectionString);
        return new MongoClient(connectionString);
    }

    public IMongoDatabase GetMongoDatabase() => CreateMongoClient().GetDatabase(MongoDatabase);
}

