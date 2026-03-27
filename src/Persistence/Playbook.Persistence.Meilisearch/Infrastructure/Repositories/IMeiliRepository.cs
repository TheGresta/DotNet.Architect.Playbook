using Playbook.Persistence.Meilisearch.Core.Models;
using Playbook.Persistence.Meilisearch.Infrastructure.Client;

namespace Playbook.Persistence.Meilisearch.Infrastructure.Repositories;

/// <summary>
/// A high-performance, generic repository for Meilisearch operations.
/// </summary>
public interface IMeiliRepository<T> where T : class
{
    Task<SearchResponse<T>> SearchAsync(string? query, Action<MeiliSearchDescriptor<T>>? configure = null, CancellationToken ct = default);
    Task AddOrUpdateAsync(IEnumerable<T> documents, CancellationToken ct = default);
    Task DeleteAsync(IEnumerable<string> ids, CancellationToken ct = default);
    Task TruncateAsync(CancellationToken ct = default);
}
