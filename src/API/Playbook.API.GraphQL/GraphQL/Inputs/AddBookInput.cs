namespace Playbook.API.GraphQL.GraphQL.Inputs;

public record AddBookInput(
    Guid AuthorId,
    string Title,
    Genre Genre,
    int PublishedYear,
    double Rating,
    int PageCount,
    string Isbn);
