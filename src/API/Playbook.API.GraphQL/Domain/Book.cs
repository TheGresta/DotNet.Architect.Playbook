namespace Playbook.API.GraphQL.Domain;

public sealed record Book(
    Guid Id,
    Guid AuthorId,
    string Title,
    Genre Genre,
    int PublishedYear,
    double Rating,
    int PageCount,
    string Isbn,
    DateTime CreatedAtUtc);
