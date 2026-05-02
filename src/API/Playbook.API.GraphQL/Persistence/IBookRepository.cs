namespace Playbook.API.GraphQL.Persistence;

public interface IBookRepository
{
    IQueryable<Book> GetAll();
    IQueryable<Book> GetByAuthorId(Guid authorId);
    Task<Book?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Book> AddAsync(Book book, CancellationToken ct);
}
