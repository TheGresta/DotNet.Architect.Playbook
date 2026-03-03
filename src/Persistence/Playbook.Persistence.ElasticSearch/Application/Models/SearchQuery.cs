namespace Playbook.Persistence.ElasticSearch.Application.Models;

public record SearchQuery(
    string? Term = null,
    int Page = 1,
    int PageSize = 10,
    Dictionary<string, object>? Filters = null,
    string? SortBy = null,
    bool SortDescending = true);