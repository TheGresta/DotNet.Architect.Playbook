using System.Collections.Concurrent;

namespace Playbook.API.GraphQL.Persistence;

public sealed class InMemoryAuthorRepository : IAuthorRepository
{
    private readonly ConcurrentDictionary<Guid, Author> _store;

    public InMemoryAuthorRepository(IEnumerable<Author> seed)
        => _store = new ConcurrentDictionary<Guid, Author>(seed.ToDictionary(a => a.Id));

    public IQueryable<Author> GetAll()
        => _store.Values.AsQueryable();

    public Task<Author?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        _store.TryGetValue(id, out var author);
        return Task.FromResult(author);
    }

    public Task<IReadOnlyList<Author>> GetByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        IReadOnlyList<Author> result = ids
            .Select(id => _store.TryGetValue(id, out var a) ? a : null)
            .OfType<Author>()
            .ToList();
        return Task.FromResult(result);
    }

    public Task<Author> AddAsync(Author author, CancellationToken ct)
    {
        _store[author.Id] = author;
        return Task.FromResult(author);
    }
}
