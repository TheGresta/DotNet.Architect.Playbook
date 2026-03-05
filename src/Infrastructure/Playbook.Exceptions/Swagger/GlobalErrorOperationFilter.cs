using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Playbook.Exceptions.Swagger;

public class GlobalErrorOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Define the error response schema reference
        var errorSchema = context.SchemaGenerator.GenerateSchema(typeof(ApiErrorResponse), context.SchemaRepository);

        // 400 Bad Request (Validation)
        AddResponse(operation, "400", "Validation Error / Bad Request", errorSchema);

        // 404 Not Found
        AddResponse(operation, "404", "The requested resource was not found", errorSchema);

        // 422 Unprocessable Entity (Business Rules)
        AddResponse(operation, "422", "Business Rule Violation", errorSchema);

        // 500 Internal Server Error
        AddResponse(operation, "500", "Internal Server Error / Unexpected Failure", errorSchema);
    }

    private static void AddResponse(OpenApiOperation operation, string statusCode, string description, OpenApiSchema schema)
    {
        if (!operation.Responses.ContainsKey(statusCode))
        {
            operation.Responses.Add(statusCode, new OpenApiResponse
            {
                Description = description,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new() { Schema = schema }
                }
            });
        }
    }
}
