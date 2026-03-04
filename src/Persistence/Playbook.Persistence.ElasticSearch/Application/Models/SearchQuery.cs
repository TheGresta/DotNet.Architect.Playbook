using System.Linq.Expressions;

namespace Playbook.Persistence.ElasticSearch.Application.Models;

public class SearchQuery<T> where T : BaseDocument
{
    public string? Term { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public Expression<Func<T, object>>? SortByExpression { get; set; }
    public bool SortDescending { get; set; }
    public Dictionary<Expression<Func<T, object>>, object> Filters { get; set; } = [];
    public int Skip => (Page - 1) * PageSize;
}