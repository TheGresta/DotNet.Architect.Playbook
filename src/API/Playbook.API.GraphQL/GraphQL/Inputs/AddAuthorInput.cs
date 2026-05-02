namespace Playbook.API.GraphQL.GraphQL.Inputs;

public record AddAuthorInput(
    string FirstName,
    string LastName,
    string Nationality,
    DateOnly BirthDate);
