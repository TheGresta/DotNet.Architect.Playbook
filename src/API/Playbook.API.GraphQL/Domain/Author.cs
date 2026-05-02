namespace Playbook.API.GraphQL.Domain;

public sealed record Author(
    Guid Id,
    string FirstName,
    string LastName,
    string Nationality,
    DateOnly BirthDate,
    DateTime CreatedAtUtc)
{
    public string FullName => $"{FirstName} {LastName}";
}
