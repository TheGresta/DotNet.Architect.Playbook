namespace Playbook.Persistence.Meilisearch.Core.Models;

public record SearchResponse<T>(
    IReadOnlyList<T> Items,
    long TotalCount,
    int? Limit,
    int? Offset,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>>? Facets
);
