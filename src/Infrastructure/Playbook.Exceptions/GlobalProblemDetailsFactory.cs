using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Playbook.Exceptions.Constants;

namespace Playbook.Exceptions;

public class GlobalProblemDetailsFactory(IHostEnvironment env) : ProblemDetailsFactory
{
    public override ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null)
    {
        var status = statusCode ?? 500;

        // Logic to set a default title for 422 if not provided
        var finalTitle = status == StatusCodes.Status422UnprocessableEntity && string.IsNullOrEmpty(title)
            ? ErrorCodes.BusinessRuleViolation
            : title;

        return new ApiErrorResponse
        {
            Status = status,
            Title = finalTitle ?? "An error occurred",
            Detail = detail,
            Instance = instance ?? httpContext.Request.Path,
            TraceId = httpContext.TraceIdentifier,
            ErrorCode = status == 422 ? ErrorCodes.BusinessRuleViolation : null
        };
    }

    public override ValidationProblemDetails CreateValidationProblemDetails(
        HttpContext httpContext,
        ModelStateDictionary modelStateDictionary,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null)
    {
        var status = statusCode ?? StatusCodes.Status400BadRequest;

        var problemDetails = new ApiErrorResponse
        {
            Status = status,
            Title = title ?? "Validation Error",
            Detail = detail ?? "One or more validation errors occurred.",
            Instance = instance ?? httpContext.Request.Path,
            TraceId = httpContext.TraceIdentifier,
            ErrorCode = "VALIDATION_ERROR"
        };

        // Correctly map ModelState to the base Errors dictionary
        foreach (var modelStateKey in modelStateDictionary.Keys)
        {
            var entry = modelStateDictionary[modelStateKey];
            if (entry is not null && entry.Errors.Count > 0)
            {
                problemDetails.Errors.Add(
                    modelStateKey,
                    [.. entry.Errors.Select(e => e.ErrorMessage)]);
            }
        }

        return problemDetails;
    }
}
