using System.Reflection;
using MongoDB.Driver;
using Playbook.Persistence.MongoDB.Domain;
using Playbook.Persistence.MongoDB.Infrastructure.Contexts;
using Polly;

namespace Playbook.Persistence.MongoDB.Infrastructure;

internal class MongoDbResilientSeederService(
    IServiceScopeFactory scopeFactory,
    ILogger<MongoDbResilientSeederService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("MongoDB Dynamic Seeding Engine started.");

        await Policy.Handle<Exception>()
            .WaitAndRetryAsync(5, retry => TimeSpan.FromSeconds(Math.Pow(2, retry)),
                (ex, time) => logger.LogWarning("Seeding attempt failed. Retrying in {Delay}s...", time.TotalSeconds))
            .ExecuteAsync(async () =>
            {
                using IServiceScope scope = scopeFactory.CreateScope();
                MongoDbContext context = scope.ServiceProvider.GetRequiredService<MongoDbContext>();

                var configTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(t => !t.IsInterface && !t.IsAbstract &&
                                t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDocumentConfiguration<>)))
                    .ToList();

                foreach (Type? configType in configTypes)
                {
                    Type interfaceType = configType.GetInterfaces().First(i => i.GetGenericTypeDefinition() == typeof(IDocumentConfiguration<>));
                    Type documentType = interfaceType.GetGenericArguments()[0];

                    // THE BRIDGE: Call the generic method once
                    await InvokeGenericProcessAsync(context, documentType, configType, ct);
                }
            });

        logger.LogInformation("All documents seeded and indexed successfully.");
    }

    private async Task InvokeGenericProcessAsync(MongoDbContext context, Type documentType, Type configType, CancellationToken ct)
    {
        MethodInfo method = GetType().GetMethod(
            nameof(ProcessInternalAsync),
            BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(documentType, configType);

        await (Task)method.Invoke(null, [context, ct])!;
    }

    private static async Task ProcessInternalAsync<TDocument, TConfig>(MongoDbContext context, CancellationToken ct)
        where TDocument : BaseDocument
        where TConfig : IDocumentConfiguration<TDocument>, new()
    {
        IMongoCollection<TDocument> collection = context.Database.GetCollection<TDocument>(typeof(TDocument).Name);
        var config = new TConfig();

        // A. Handle Indexes
        var indexModels = config.ConfigureIndexes(Builders<TDocument>.IndexKeys).ToList();
        if (indexModels.Count > 0)
        {
            await collection.Indexes.CreateManyAsync(indexModels, ct);
        }

        // B. Handle Seed Data
        if (!await collection.Find(Builders<TDocument>.Filter.Empty).AnyAsync(ct))
        {
            var seedData = config.SeedData().ToList();
            if (seedData.Count > 0)
            {
                await collection.InsertManyAsync(seedData, cancellationToken: ct);
            }
        }
    }
}
