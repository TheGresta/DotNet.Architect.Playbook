using Microsoft.OpenApi.Models;

using Playbook.Exceptions.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace Playbook.Exceptions.Infrastructure;

/// <summary>
/// A Swashbuckle OpenAPI operation filter that globally injects standardized error response 
/// definitions into all API documentation. This ensures the Swagger UI accurately reflects 
/// the <see cref="ApiErrorResponse"/> structure for common HTTP failure codes.
/// </summary>
public sealed class GlobalErrorOperationFilter : IOperationFilter
{
    /// <summary>
    /// Applies the filter to a specific API operation, registering standard 4xx and 5xx responses.
    /// </summary>
    /// <param name="operation">The OpenAPI operation instance being processed.</param>
    /// <param name="context">The filter context providing access to schema generation and repository.</param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Generate the schema once for re-use across multiple status code definitions 
        // to minimize reflection and generation overhead.
        var errorSchema = context.SchemaGenerator.GenerateSchema(typeof(ApiErrorResponse), context.SchemaRepository);

        // Standardized Error Mapping across the entire API surface.
        // These calls ensure that consumers of the OpenAPI spec know exactly what the error payload looks like.
        AddResponse(operation, "400", "Validation Error or Malformed Request", errorSchema);
        AddResponse(operation, "401", "Unauthorized - Authentication required", errorSchema);
        AddResponse(operation, "403", "Forbidden - Insufficient permissions", errorSchema);
        AddResponse(operation, "404", "Resource not found", errorSchema);
        AddResponse(operation, "422", "Business Rule Violation", errorSchema);
        AddResponse(operation, "500", "Internal Server Error", errorSchema);
    }

    /// <summary>
    /// Helper method to safely register a response type within the OpenAPI operation.
    /// </summary>
    /// <param name="operation">The operation to modify.</param>
    /// <param name="statusCode">The HTTP status code string (e.g., "404").</param>
    /// <param name="description">The human-readable description for the OpenAPI documentation.</param>
    /// <param name="schema">The schema representing the error payload structure.</param>
    private static void AddResponse(OpenApiOperation operation, string statusCode, string description, OpenApiSchema schema) =>
        // Use TryAdd to avoid potential key-collision exceptions if a specific endpoint 
        // already has an explicit [ProducesResponseType] attribute for the same status code.
        _ = operation.Responses.TryAdd(statusCode, new OpenApiResponse
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                // Content types registered for compatibility with RFC 7807 (Problem Details) and standard JSON.
                ["application/problem+json"] = new() { Schema = schema },
                ["application/json"] = new() { Schema = schema }
            }
        });
}
