using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using Playbook.Exceptions.Abstraction;
using Playbook.Exceptions.Constants;
using Playbook.Exceptions.Models;

namespace Playbook.Exceptions.Core;

public sealed class GlobalProblemDetailsFactory(
    ILocalizedStringProvider stringProvider) : ProblemDetailsFactory
{
    public override ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null)
    {
        var status = statusCode ?? StatusCodes.Status500InternalServerError;

        return new ApiErrorResponse
        {
            Status = status,
            Title = title ?? GetTitleForStatus(status),
            Detail = detail ?? GetDetailForStatus(status),
            Instance = instance ?? httpContext.Request.Path,
            TraceId = httpContext.TraceIdentifier,
            ErrorCode = GetErrorCodeForStatus(status)
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

        var response = new ApiErrorResponse
        {
            Status = status,
            Title = title ?? stringProvider.Get(TitleKeys.ValidationError),
            Detail = detail ?? stringProvider.Get(DetailKeys.ValidationSummary),
            Instance = instance ?? httpContext.Request.Path,
            TraceId = httpContext.TraceIdentifier,
            ErrorCode = ErrorCodes.ValidationError,
            Errors = new Dictionary<string, string[]>(modelStateDictionary.Count)
        };

        foreach (var (key, entry) in modelStateDictionary)
        {
            if (entry.Errors.Count == 0) continue;

            var errorMessages = new string[entry.Errors.Count];
            for (var i = 0; i < entry.Errors.Count; i++)
            {
                // Localization protocol: Framework messages (like "The field is required") 
                // are passed through the provider for potential custom overrides.
                errorMessages[i] = stringProvider.Get(entry.Errors[i].ErrorMessage);
            }

            response.Errors.TryAdd(key, errorMessages);
        }

        return response;
    }

    private string GetTitleForStatus(int status) => status switch
    {
        StatusCodes.Status401Unauthorized => stringProvider.Get(TitleKeys.Unauthorized),
        StatusCodes.Status403Forbidden => stringProvider.Get(TitleKeys.Unauthorized),
        StatusCodes.Status404NotFound => stringProvider.Get(TitleKeys.NotFound),
        StatusCodes.Status422UnprocessableEntity => stringProvider.Get(TitleKeys.BusinessRule),
        _ => stringProvider.Get(TitleKeys.InternalServer)
    };

    private string GetDetailForStatus(int status) => status switch
    {
        StatusCodes.Status401Unauthorized => stringProvider.Get(DetailKeys.Unauthorized),
        StatusCodes.Status403Forbidden => stringProvider.Get(DetailKeys.Unauthorized),
        StatusCodes.Status404NotFound => stringProvider.Get(DetailKeys.NotFound), // Optional: add generic "Resource not found"
        _ => stringProvider.Get(DetailKeys.UnexpectedError)
    };

    private static string GetErrorCodeForStatus(int status) => status switch
    {
        StatusCodes.Status401Unauthorized => ErrorCodes.Unauthorized,
        StatusCodes.Status403Forbidden => ErrorCodes.Unauthorized,
        StatusCodes.Status422UnprocessableEntity => ErrorCodes.BusinessRuleViolation,
        StatusCodes.Status400BadRequest => ErrorCodes.ValidationError,
        StatusCodes.Status404NotFound => ErrorCodes.NotFound,
        _ => ErrorCodes.InternalServerError
    };
}
