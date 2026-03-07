using Microsoft.OpenApi.Models;

using Playbook.Exceptions.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace Playbook.Exceptions.Infrastructure;

public sealed class GlobalErrorOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Generate the schema once for re-use
        var errorSchema = context.SchemaGenerator.GenerateSchema(typeof(ApiErrorResponse), context.SchemaRepository);

        // Standardized Error Mapping
        AddResponse(operation, "400", "Validation Error or Malformed Request", errorSchema);
        AddResponse(operation, "401", "Unauthorized - Authentication required", errorSchema);
        AddResponse(operation, "403", "Forbidden - Insufficient permissions", errorSchema);
        AddResponse(operation, "404", "Resource not found", errorSchema);
        AddResponse(operation, "422", "Business Rule Violation", errorSchema);
        AddResponse(operation, "500", "Internal Server Error", errorSchema);
    }

    private static void AddResponse(OpenApiOperation operation, string statusCode, string description, OpenApiSchema schema) =>
        // Use TryAdd to avoid potential key-collision exceptions
        _ = operation.Responses.TryAdd(statusCode, new OpenApiResponse
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/problem+json"] = new() { Schema = schema },
                ["application/json"] = new() { Schema = schema }
            }
        });
}
