namespace Playbook.API.GraphQL.GraphQL.Queries;

public sealed class Query
{
    // Attribute order is critical: UsePaging must wrap UseFiltering which wraps UseSorting.
    // HC evaluates decorators outermost-first; reversing this breaks totalCount.
    [UsePaging(IncludeTotalCount = true, DefaultPageSize = 10, MaxPageSize = 100)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Book> GetBooks([Service] IBookRepository repo) =>
        repo.GetAll();

    [UsePaging(IncludeTotalCount = true, DefaultPageSize = 10, MaxPageSize = 100)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Author> GetAuthors([Service] IAuthorRepository repo) =>
        repo.GetAll();

    public Task<Book?> GetBookById(
        Guid id,
        [Service] IBookRepository repo,
        CancellationToken ct) =>
        repo.GetByIdAsync(id, ct);

    public Task<Author?> GetAuthorById(
        Guid id,
        [Service] IAuthorRepository repo,
        CancellationToken ct) =>
        repo.GetByIdAsync(id, ct);
}
