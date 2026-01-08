using DotNet.Testcontainers.Builders;
using MongoDB.Driver;
using Serilog;
using IContainer = DotNet.Testcontainers.Containers.IContainer;

namespace Akka.Pathfinder.AcceptanceTests.Containers;

public class MongoDbContainer : IAsyncLifetime
{
    private const int InternalPort = 27017;
    private const string MongoDatabase = "pathfinder";
    private const string MongoUser = "admin";
    private const string MongoPassword = "password";
    private const string KeyFileContent = "thisisakeyfileforreplicasetauth12345678";
    private const string KeyFilePath = "/data/configdb/keyfile";

    public MongoDbContainer()
    {
        Log.Information("[TEST][{MongoDbContainerName}] ctor", GetType().Name);
        
        var keyFileCommand = $"echo '{KeyFileContent}' > /tmp/keyfile && chmod 400 /tmp/keyfile && exec mongod --replSet rs0 --bind_ip_all --keyFile /tmp/keyfile";

        Container = new ContainerBuilder()
            .WithImage("mongo:6.0")
            .WithAutoRemove(true)
            .WithPortBinding(InternalPort, true)
            .WithEntrypoint("sh", "-c", keyFileCommand)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilMessageIsLogged("Waiting for connections"))
            .Build();
    }

    public int Port { get; private set; }

    public string Hostname { get; private set; } = string.Empty;

    public IContainer Container { get; init; }

    public string GetConnectionString()
    {
        return $"mongodb://{MongoUser}:{MongoPassword}@{Hostname}:{Port}/{MongoDatabase}?authSource=admin&replicaSet=rs0&directConnection=true";
    }

    public async Task DropDataAsync()
    {
        Log.Information("[TEST][{MongoDbContainerName}] DropDataAsync", GetType().Name);

        var connectionString = $"mongodb://{MongoUser}:{MongoPassword}@{Hostname}:{Port}/?authSource=admin&replicaSet=rs0&directConnection=true";
        var mongoClient = new MongoClient(connectionString);

        await mongoClient.DropDatabaseAsync(MongoDatabase);
        Log.Information("[TEST][{MongoDbContainerName}] DropDataAsync finished", GetType().Name);
    }

    public async Task InitializeAsync()
    {
        Log.Information("[TEST][{MongoDbContainerName}] InitializeAsync", GetType().Name);

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await Container.StartAsync(timeoutCts.Token).ConfigureAwait(false);

        Port = Container.GetMappedPublicPort(InternalPort);
        Hostname = Container.Hostname;

        await InitiateReplicaSetAsync().ConfigureAwait(false);
        await CreateUserAsync().ConfigureAwait(false);

        Log.Information("[TEST][{MongoDbContainerName}] started and ready on Port [{Hostname}:{PublicPort}]",
            GetType().Name, Hostname, Port);

        Log.Information("[TEST][{MongoDbContainerName}] InitializeAsync finished", GetType().Name);
    }

    private async Task InitiateReplicaSetAsync()
    {
        Log.Information("[TEST][{MongoDbContainerName}] Initiate replica set", GetType().Name);

        var initiateCommand = "rs.initiate({_id:'rs0', members:[{_id:0, host:'localhost:27017'}]})";

        for (var i = 0; i < 30; i++)
        {
            var result = await Container.ExecAsync(new[] { "mongosh", "--eval", initiateCommand }).ConfigureAwait(false);
            if (result.ExitCode == 0)
            {
                Log.Information("[TEST][{MongoDbContainerName}] Replica set initiated", GetType().Name);
                await WaitForPrimaryAsync().ConfigureAwait(false);
                return;
            }

            if (i % 5 == 0)
            {
                Log.Warning("[TEST][{MongoDbContainerName}] Attempt {Attempt} to initiate replica set failed: {Stderr}",
                    GetType().Name, i + 1, result.Stderr);
            }

            await Task.Delay(1000).ConfigureAwait(false);
        }

        throw new Exception("Failed to initiate replica set after multiple attempts");
    }

    private async Task CreateUserAsync()
    {
        Log.Information("[TEST][{MongoDbContainerName}] Create admin user", GetType().Name);
        var createUserCommand = $@"
        db.getSiblingDB('admin').createUser({{
            user: '{MongoUser}',
            pwd: '{MongoPassword}',
            roles: [{{ role: 'root', db: 'admin' }}]
        }})";

        var result = await Container.ExecAsync(new[] { "mongosh", "--eval", createUserCommand }).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            Log.Error("[TEST][{MongoDbContainerName}] Failed to create user: {Stderr}", GetType().Name, result.Stderr);
            throw new Exception("Failed to create admin user");
        }

        Log.Information("[TEST][{MongoDbContainerName}] Admin user created", GetType().Name);
    }

    private async Task WaitForPrimaryAsync()
    {
        Log.Information("[TEST][{MongoDbContainerName}] Waiting for node to become primary", GetType().Name);
        for (var i = 0; i < 30; i++)
        {
            var result = await Container.ExecAsync(new[] { "mongosh", "--quiet", "--eval", "db.hello().isWritablePrimary" })
                .ConfigureAwait(false);
            if (result.ExitCode == 0 && result.Stdout.Trim().ToLower().Contains("true"))
            {
                Log.Information("[TEST][{MongoDbContainerName}] Node is primary", GetType().Name);
                return;
            }

            await Task.Delay(1000).ConfigureAwait(false);
        }

        Log.Warning("[TEST][{MongoDbContainerName}] Node did not become primary in time, continuing anyway",
            GetType().Name);
    }

    public async Task DisposeAsync()
    {
        Log.Information("[TEST][{MongoDbContainerName}] DisposeAsync", GetType().Name);
        await Container.StopAsync();
        await Container.DisposeAsync();
        Log.Information("[TEST][{MongoDbContainerName}] DisposeAsync finished", GetType().Name);
    }

    public IMongoClient CreateMongoClient()
    {
        var connectionString = $"mongodb://{MongoUser}:{MongoPassword}@{Hostname}:{Port}/?authSource=admin&replicaSet=rs0&directConnection=true";
        Log.Debug("[TEST][{MongoDbContainerName}] MongoDB ConnectionString: {connectionString}", GetType().Name,
            connectionString);
        return new MongoClient(connectionString);
    }

    public IMongoDatabase GetMongoDatabase() => CreateMongoClient().GetDatabase(MongoDatabase);
}