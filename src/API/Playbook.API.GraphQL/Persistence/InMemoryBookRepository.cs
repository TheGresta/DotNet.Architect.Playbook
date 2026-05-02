using System.Collections.Concurrent;

namespace Playbook.API.GraphQL.Persistence;

public sealed class InMemoryBookRepository : IBookRepository
{
    private readonly ConcurrentDictionary<Guid, Book> _store;

    public InMemoryBookRepository(IEnumerable<Book> seed)
        => _store = new ConcurrentDictionary<Guid, Book>(seed.ToDictionary(b => b.Id));

    public IQueryable<Book> GetAll()
        => _store.Values.AsQueryable();

    public IQueryable<Book> GetByAuthorId(Guid authorId)
        => _store.Values.Where(b => b.AuthorId == authorId).AsQueryable();

    public Task<Book?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        _store.TryGetValue(id, out var book);
        return Task.FromResult(book);
    }

    public Task<Book> AddAsync(Book book, CancellationToken ct)
    {
        _store[book.Id] = book;
        return Task.FromResult(book);
    }
}
