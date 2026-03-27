using System.Reflection;

using Playbook.Persistence.Meilisearch.Core.Attributes;
using Playbook.Persistence.Meilisearch.Infrastructure.Client;

using MeiliIndex = Meilisearch.Index;

namespace Playbook.Persistence.Meilisearch.Infrastructure.Configuration;

/// <summary>
/// A generic base configuration that automatically discovers Meilisearch 
/// settings using [MeiliFilterable] and [MeiliSortable] attributes.
/// </summary>
public abstract class MeiliIndexConfiguration<T> : IMeiliIndexConfiguration where T : class
{
    public abstract string IndexName { get; }

    // Virtual to allow specific indices to override or add custom synonyms
    public virtual Dictionary<string, IEnumerable<string>>? GetSynonyms() => null;

    public async Task ConfigureAsync(MeiliIndex index, CancellationToken ct)
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var filterable = properties
            .Where(p => p.GetCustomAttribute<MeiliFilterableAttribute>() != null)
            .Select(MeiliFilterBuilder<T>.GetCachedPropertyName)
            .ToArray();

        var sortable = properties
            .Where(p => p.GetCustomAttribute<MeiliSortableAttribute>() != null)
            .Select(MeiliFilterBuilder<T>.GetCachedPropertyName)
            .ToArray();

        // Meilisearch handles multiple update calls by queuing them.
        await index.UpdateFilterableAttributesAsync(filterable, ct).ConfigureAwait(false);
        await index.UpdateSortableAttributesAsync(sortable, ct).ConfigureAwait(false);

        var synonyms = GetSynonyms();
        if (synonyms is { Count: > 0 })
        {
            await index.UpdateSynonymsAsync(synonyms, ct).ConfigureAwait(false);
        }
    }
}
