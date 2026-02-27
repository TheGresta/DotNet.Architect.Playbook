using MongoDB.Driver;

using Playbook.Persistence.MongoDB.Domain.Documents;

namespace Playbook.Persistence.MongoDB.Infrastructure.Configs;

internal class ExceptionMessageConfig : IDocumentConfiguration<ExceptionMessageDocument>
{
    public IEnumerable<CreateIndexModel<ExceptionMessageDocument>> ConfigureIndexes(IndexKeysDefinitionBuilder<ExceptionMessageDocument> builder)
    {
        yield return new CreateIndexModel<ExceptionMessageDocument>(
            builder.Ascending(x => x.Code),
            new CreateIndexOptions { Unique = true });
    }

    public IEnumerable<ExceptionMessageDocument> SeedData() => [
        new()
        {
            Code = "invalid_request_format",
            Message = "The request could not be processed because it is malformed or contains invalid parameters. Please verify the request structure and try again."
        },
        new()
        {
            Code = "authentication_failed",
            Message = "Authentication failed. The provided credentials are incorrect or have expired. Please log in again and retry."
        },
        new()
        {
            Code = "authorization_denied",
            Message = "You do not have permission to perform this action. Please contact your administrator if you believe this is an error."
        },
        new()
        {
            Code = "resource_not_found",
            Message = "The requested resource could not be found. It may have been removed or the identifier provided is incorrect."
        }
    ];
}
