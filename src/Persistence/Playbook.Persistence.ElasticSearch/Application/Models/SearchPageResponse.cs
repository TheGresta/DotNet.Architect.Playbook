namespace Playbook.Persistence.ElasticSearch.Application.Models;

public record SearchPageResponse<T>(
    IReadOnlyCollection<T> Items,
    long TotalCount,
    int CurrentPage,
    int PageSize,
    TimeSpan ExecutionTime) where T : BaseDocument
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => CurrentPage < TotalPages;
}
