using Meilisearch;

using Playbook.Persistence.Meilisearch.Core.Models;

using MeiliIndex = Meilisearch.Index;

namespace Playbook.Persistence.Meilisearch.Infrastructure.Client;

/// <summary>
/// A high-level wrapper for Meilisearch operations.
/// Acts as the primary entry point for the persistence layer.
/// </summary>
public sealed class MeiliContext(
    MeilisearchClient client,
    ILogger<MeiliContext> logger)
{
    private readonly MeilisearchClient _client = client ?? throw new ArgumentNullException(nameof(client));

    /// <summary>
    /// Returns the typed Index handle. 
    /// We use the 'MeiliIndex' alias to resolve the CS0118 ambiguity.
    /// </summary>
    public MeiliIndex GetIndex() => _client.Index(MeiliConstants.IndexName);

    public async Task InitializeSettingsAsync(CancellationToken ct = default)
    {
        var index = GetIndex();

        // Define synonyms to bridge the gap between user intent and messy data
        var synonyms = new Dictionary<string, IEnumerable<string>>
        {
            { "hybrid", ["hyrbrid", "plug in hybrid"] },
            { "electric", ["ev", "bev"] },
            { "petrol", ["gasoline", "benzin"] }
        };

        try
        {
            // 1. Update Attributes
            await index.UpdateFilterableAttributesAsync(MeiliConstants.FilterableAttributes, ct);

            // 2. Update Synonyms (Fixes the "hyrbrid" typo search)
            var sTask = await index.UpdateSynonymsAsync(synonyms, ct);

            // 3. Wait for Task Completion
            await _client.WaitForTaskAsync(sTask.TaskUid, cancellationToken: ct);

            logger.LogInformation("Synonyms and Settings synchronized successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Setup failed during synonym synchronization.");
            throw;
        }
    }
}
