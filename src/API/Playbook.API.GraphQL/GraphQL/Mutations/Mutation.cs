namespace Playbook.API.GraphQL.GraphQL.Mutations;

public sealed class Mutation
{
    public async Task<Author> AddAuthorAsync(
        AddAuthorInput input,
        [Service] IAuthorRepository repo,
        CancellationToken ct) =>
        await repo.AddAsync(
            new Author(Guid.NewGuid(), input.FirstName, input.LastName,
                input.Nationality, input.BirthDate, DateTime.UtcNow),
            ct);

    public async Task<Book> AddBookAsync(
        AddBookInput input,
        [Service] IAuthorRepository authorRepo,
        [Service] IBookRepository bookRepo,
        CancellationToken ct)
    {
        var authorExists = await authorRepo.GetByIdAsync(input.AuthorId, ct);
        if (authorExists is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage($"Author '{input.AuthorId}' was not found.")
                    .SetCode("AUTHOR_NOT_FOUND")
                    .Build());
        }

        return await bookRepo.AddAsync(
            new Book(Guid.NewGuid(), input.AuthorId, input.Title, input.Genre,
                input.PublishedYear, input.Rating, input.PageCount, input.Isbn, DateTime.UtcNow),
            ct);
    }
}
