namespace Playbook.API.GraphQL.Persistence;

public interface IAuthorRepository
{
    IQueryable<Author> GetAll();
    Task<Author?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Author>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct);
    Task<Author> AddAsync(Author author, CancellationToken ct);
}
