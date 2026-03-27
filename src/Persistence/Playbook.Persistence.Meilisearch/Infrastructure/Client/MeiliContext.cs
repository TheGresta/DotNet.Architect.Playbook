using Meilisearch;

using Playbook.Persistence.Meilisearch.Core.Constants;
using Playbook.Persistence.Meilisearch.Infrastructure.Configuration;

using MeiliIndex = Meilisearch.Index;

namespace Playbook.Persistence.Meilisearch.Infrastructure.Client;

/// <summary>
/// Provides a high-level architectural wrapper for Meilisearch operations, acting as a specialized 
/// unit of work for search index management. This class orchestrates the discovery and initialization 
/// of all registered index configurations within the service collection.
/// </summary>
/// <remarks>
/// The <see cref="MeiliContext"/> ensures that the search engine state is synchronized with the 
/// application's domain requirements during startup or maintenance windows.
/// </remarks>
public sealed class MeiliContext(
    MeilisearchClient client,
    IEnumerable<IMeiliIndexConfiguration> configurations,
    ILogger<MeiliContext> logger)
{
    /// <summary>
    /// The underlying Meilisearch high-level client instance.
    /// </summary>
    private readonly MeilisearchClient _client = client ?? throw new ArgumentNullException(nameof(client));

    /// <summary>
    /// A collection of domain-specific index configurations used to define settings, rankings, and filterable attributes.
    /// </summary>
    private readonly IEnumerable<IMeiliIndexConfiguration> _configurations = configurations ?? throw new ArgumentNullException(nameof(configurations));

    /// <summary>
    /// Resolves and returns a specific <see cref="MeiliIndex"/> handle by name.
    /// </summary>
    /// <param name="indexName">The unique identifier of the index. If null, falls back to the default system index name.</param>
    /// <returns>An instance of <see cref="MeiliIndex"/> for executing search or document operations.</returns>
    public MeiliIndex GetIndex(string? indexName = null)
        => _client.Index(indexName ?? MeiliConstants.IndexName);

    /// <summary>
    /// Iterates through all registered <see cref="IMeiliIndexConfiguration"/> instances to apply 
    /// settings such as stop words, synonyms, and ranking rules to the Meilisearch instance.
    /// </summary>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous synchronization operation.</returns>
    /// <exception cref="Exception">Thrown and logged if a specific index configuration fails to apply, preventing invalid engine states.</exception>
    public async Task InitializeSettingsAsync(CancellationToken ct = default)
    {
        // ── Phase 1: snapshot current settings for every index ──────────────
        var snapshots = new Dictionary<string, Settings>();

        foreach (var config in _configurations)
        {
            var index = _client.Index(config.IndexName);
            try
            {
                var currentSettings = await index.GetSettingsAsync(ct).ConfigureAwait(false);
                snapshots[config.IndexName] = currentSettings;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Cannot snapshot settings for index: {IndexName}", config.IndexName);
                throw;   // Can't proceed safely without a baseline.
            }
        }

        // ── Phase 2: apply configurations, roll back on failure ─────────────
        var applied = new List<string>();   // tracks successfully updated indexes

        foreach (var config in _configurations)
        {
            var index = _client.Index(config.IndexName);
            try
            {
                logger.LogInformation("Synchronizing Meilisearch settings for index: {IndexName}", config.IndexName);

                await config.ConfigureAsync(index, ct).ConfigureAwait(false);
                applied.Add(config.IndexName);

                logger.LogInformation("Index {IndexName} synchronized successfully.", config.IndexName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Setup failed for index: {IndexName}. Rolling back {Count} previously applied index(es).",
                    config.IndexName, applied.Count);

                // ── Rollback phase ───────────────────────────────────────────
                foreach (var appliedName in applied)
                {
                    if (!snapshots.TryGetValue(appliedName, out var snapshot))
                        continue;

                    try
                    {
                        var revertIndex = _client.Index(appliedName);
                        await revertIndex.UpdateSettingsAsync(snapshot, ct).ConfigureAwait(false);
                        logger.LogWarning("Rolled back index: {IndexName}", appliedName);
                    }
                    catch (Exception rollbackEx)
                    {
                        // Log but don't suppress the original exception.
                        logger.LogCritical(rollbackEx,
                            "Rollback FAILED for index: {IndexName}. Manual intervention may be required.",
                            appliedName);
                    }
                }

                throw;  // Re-throw the original failure to halt bootstrap.
            }
        }
    }
}
