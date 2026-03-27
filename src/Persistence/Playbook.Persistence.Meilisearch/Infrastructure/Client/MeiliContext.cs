using Meilisearch;

using Playbook.Persistence.Meilisearch.Core.Constants;
using Playbook.Persistence.Meilisearch.Infrastructure.Configuration;

using MeiliIndex = Meilisearch.Index;


/// <summary>
/// A high-level wrapper for Meilisearch operations.
/// Orchestrates the initialization of all registered index configurations.
/// </summary>
public sealed class MeiliContext(
    MeilisearchClient client,
    IEnumerable<IMeiliIndexConfiguration> configurations,
    ILogger<MeiliContext> logger)
{
    private readonly MeilisearchClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly IEnumerable<IMeiliIndexConfiguration> _configurations = configurations ?? throw new ArgumentNullException(nameof(configurations));

    public MeiliIndex GetIndex(string? indexName = null)
        => _client.Index(indexName ?? MeiliConstants.IndexName);

    public async Task InitializeSettingsAsync(CancellationToken ct = default)
    {
        foreach (var config in _configurations)
        {
            try
            {
                logger.LogInformation("Synchronizing Meilisearch settings for index: {IndexName}", config.IndexName);

                var index = _client.Index(config.IndexName);
                await config.ConfigureAsync(index, ct).ConfigureAwait(false);

                logger.LogInformation("Index {IndexName} synchronized successfully.", config.IndexName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Setup failed during synchronization for index: {IndexName}", config.IndexName);
                throw; // Re-throw to prevent the application from starting in an invalid state
            }
        }
    }
}
