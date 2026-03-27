using System.Reflection;

using Playbook.Persistence.Meilisearch.Core.Attributes;
using Playbook.Persistence.Meilisearch.Infrastructure.Client;

using MeiliIndex = Meilisearch.Index;

namespace Playbook.Persistence.Meilisearch.Infrastructure.Configuration;

/// <summary>
/// Provides a high-level, abstract base implementation for <see cref="IMeiliIndexConfiguration"/>.
/// This class automates the discovery of Meilisearch settings by reflecting on the document model <typeparamref name="T"/>,
/// specifically looking for <see cref="MeiliFilterableAttribute"/> and <see cref="MeiliSortableAttribute"/> markers.
/// </summary>
/// <typeparam name="T">The document model type used to derive index settings via reflection and attributes.</typeparam>
public abstract class MeiliIndexConfiguration<T> : IMeiliIndexConfiguration where T : class
{
    /// <summary>
    /// Gets the unique identifier (UID) for the index. Must be implemented by the derived class.
    /// </summary>
    public abstract string IndexName { get; }

    /// <summary>
    /// Provides an optional dictionary of synonyms to be used by the Meilisearch engine for this specific index.
    /// </summary>
    /// <returns>A dictionary where the key is a word and the value is a collection of its synonyms, or null.</returns>
    public virtual Dictionary<string, IEnumerable<string>>? GetSynonyms() => null;

    /// <summary>
    /// Automatically configures the Meilisearch index by scanning the properties of <typeparamref name="T"/> 
    /// for search-specific metadata and applying them to the engine.
    /// </summary>
    /// <param name="index">The <see cref="MeiliIndex"/> instance to configure.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous configuration operation.</returns>
    public async Task ConfigureAsync(MeiliIndex index, CancellationToken ct)
    {
        // Retrieves all public instance properties to evaluate for search attributes.
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Identifies properties decorated with [MeiliFilterable] to enable faceted search and filtering.
        var filterable = properties
            .Where(p => p.GetCustomAttribute<MeiliFilterableAttribute>() != null)
            .Select(MeiliFilterBuilder<T>.GetCachedPropertyName)
            .ToArray();

        // Identifies properties decorated with [MeiliSortable] to allow order-by operations on the index.
        var sortable = properties
            .Where(p => p.GetCustomAttribute<MeiliSortableAttribute>() != null)
            .Select(MeiliFilterBuilder<T>.GetCachedPropertyName)
            .ToArray();

        // Synchronizes the filterable attributes with the Meilisearch server.
        // Meilisearch handles these update calls by queuing them as tasks in the engine.
        await index.UpdateFilterableAttributesAsync(filterable, ct).ConfigureAwait(false);

        // Synchronizes the sortable attributes.
        await index.UpdateSortableAttributesAsync(sortable, ct).ConfigureAwait(false);

        var synonyms = GetSynonyms();
        if (synonyms is { Count: > 0 })
        {
            // Applies custom synonyms to improve search relevance for domain-specific terminology.
            await index.UpdateSynonymsAsync(synonyms, ct).ConfigureAwait(false);
        }
    }
}
