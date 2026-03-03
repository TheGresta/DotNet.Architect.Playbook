namespace Playbook.Persistence.ElasticSearch.Application.Models;

public record SearchResult<T>(
    IReadOnlyCollection<T> Items,
    long TotalCount,
    TimeSpan Elapsed,
    string? ScrollId = null) where T : class;
