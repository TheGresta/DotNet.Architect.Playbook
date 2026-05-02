namespace Playbook.API.GraphQL.GraphQL.Types;

// Extends the Book record with a resolved `author` field without polluting the domain layer.
[ExtendObjectType<Book>]
public sealed class BookType
{
    public Task<Author?> GetAuthorAsync(
        [Parent] Book book,
        AuthorsByIdDataLoader authorLoader,
        CancellationToken ct) =>
        authorLoader.LoadAsync(book.AuthorId, ct);
}
