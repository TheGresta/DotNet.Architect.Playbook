using MeiliIndex = Meilisearch.Index;

namespace Playbook.Persistence.Meilisearch.Infrastructure.Configuration;

/// <summary>
/// Defines a strict architectural contract for index-specific settings, ranking rules, 
/// and synchronization logic. Implementing this interface allows for a modular, 
/// decoupled approach to managing multiple Meilisearch indices within a single application.
/// </summary>
/// <remarks>
/// This interface is typically used by the <see cref="MeiliContext"/> to ensure that 
/// each search index is correctly provisioned with its required searchable, 
/// filterable, and sortable attributes during system initialization.
/// </remarks>
public interface IMeiliIndexConfiguration
{
    /// <summary>
    /// Gets the unique identifier (UID) of the Meilisearch index this configuration pertains to.
    /// </summary>
    string IndexName { get; }

    /// <summary>
    /// Executes the asynchronous configuration logic against the provided Meilisearch index.
    /// This typically includes setting up ranking rules, stop words, synonyms, 
    /// and distinct attributes.
    /// </summary>
    /// <param name="index">The <see cref="MeiliIndex"/> instance to be configured.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous configuration operation.</returns>
    Task ConfigureAsync(MeiliIndex index, CancellationToken ct);
}
