namespace Playbook.API.GraphQL.GraphQL.Types;

// Extends the Author record with a filterable, sortable `books` field.
[ExtendObjectType<Author>]
public sealed class AuthorType
{
    [UseFiltering]
    [UseSorting]
    public IQueryable<Book> GetBooks(
        [Parent] Author author,
        [Service] IBookRepository repo) =>
        repo.GetByAuthorId(author.Id);
}
