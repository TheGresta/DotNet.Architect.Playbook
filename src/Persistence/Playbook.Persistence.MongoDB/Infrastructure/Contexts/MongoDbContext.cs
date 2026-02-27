using Microsoft.Extensions.Options;

using MongoDB.Driver;

namespace Playbook.Persistence.MongoDB.Infrastructure.Contexts;

/// <summary>
/// Provides a centralized point of access to the MongoDB client and database.
/// Acts as the "Unit of Work" container for database operations.
/// </summary>
internal class MongoDbContext
{
    public IMongoDatabase Database { get; }
    public IMongoClient Client { get; }

    // Summary: Holds the current transaction session, enabling multi-document atomicity.
    public IClientSessionHandle? Session { get; private set; }

    /// <summary>
    /// Initializes the connection using configuration options and ensures 
    /// global driver settings are applied via MongoInitializer.
    /// </summary>
    public MongoDbContext(IOptions<MongoDbOptions> options)
    {
        // Summary: Trigger the one-time global configuration before any client is created.
        MongoInitializer.Initialize();

        var settings = MongoClientSettings.FromConnectionString(options.Value.ConnectionString);

        // Summary: The MongoClient maintains an internal connection pool; 
        // it should be treated as a long-lived object.
        Client = new MongoClient(settings);
        Database = Client.GetDatabase(options.Value.DatabaseName);
    }

    /// <summary>
    /// Associates an active session (transaction) with this context.
    /// Used by repositories to ensure operations stay within the same transaction scope.
    /// </summary>
    public void SetSession(IClientSessionHandle? session) => Session = session;
}
