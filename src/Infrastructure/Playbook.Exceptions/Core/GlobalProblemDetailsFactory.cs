using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using Playbook.Exceptions.Abstraction;
using Playbook.Exceptions.Constants;
using Playbook.Exceptions.Models;

namespace Playbook.Exceptions.Core;

/// <summary>
/// A custom implementation of <see cref="ProblemDetailsFactory"/> that standardizes the creation 
/// of <see cref="ApiErrorResponse"/> throughout the ASP.NET Core middleware pipeline.
/// This factory integrates the <see cref="ILocalizedStringProvider"/> to ensure all 
/// automatically generated problem details (e.g., from [ApiController] validation) are localized.
/// </summary>
public sealed class GlobalProblemDetailsFactory(
    ILocalizedStringProvider stringProvider) : ProblemDetailsFactory
{
    /// <summary>
    /// Creates a standardized <see cref="ProblemDetails"/> (as <see cref="ApiErrorResponse"/>) 
    /// for a given set of parameters.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="statusCode">The HTTP status code to associate with the error.</param>
    /// <param name="title">The summary title of the error.</param>
    /// <param name="type">A URI reference that identifies the problem type.</param>
    /// <param name="detail">A detailed explanation of the error.</param>
    /// <param name="instance">A URI reference that identifies the specific occurrence of the problem.</param>
    /// <returns>A configured instance of <see cref="ApiErrorResponse"/>.</returns>
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

    /// <summary>
    /// Creates a <see cref="ValidationProblemDetails"/> (as <see cref="ApiErrorResponse"/>) 
    /// based on the provided <see cref="ModelStateDictionary"/>.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <param name="modelStateDictionary">The dictionary containing model validation errors.</param>
    /// <param name="statusCode">The HTTP status code. Defaults to 400.</param>
    /// <param name="title">The error title.</param>
    /// <param name="type">The problem type URI.</param>
    /// <param name="detail">The error detail message.</param>
    /// <param name="instance">The specific request path.</param>
    /// <returns>An <see cref="ApiErrorResponse"/> populated with localized validation errors.</returns>
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

        // Iterates through model state to translate framework-generated error messages.
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

            // Ensures keys are added only once to prevent dictionary collision.
            response.Errors.TryAdd(key, errorMessages);
        }

        return response;
    }

    /// <summary>
    /// Maps an HTTP status code to its corresponding localized title key.
    /// </summary>
    private string GetTitleForStatus(int status) => status switch
    {
        StatusCodes.Status401Unauthorized => stringProvider.Get(TitleKeys.Unauthorized),
        StatusCodes.Status403Forbidden => stringProvider.Get(TitleKeys.Unauthorized),
        StatusCodes.Status404NotFound => stringProvider.Get(TitleKeys.NotFound),
        StatusCodes.Status422UnprocessableEntity => stringProvider.Get(TitleKeys.BusinessRule),
        _ => stringProvider.Get(TitleKeys.InternalServer)
    };

    /// <summary>
    /// Maps an HTTP status code to its corresponding localized detail template.
    /// </summary>
    private string GetDetailForStatus(int status) => status switch
    {
        StatusCodes.Status401Unauthorized => stringProvider.Get(DetailKeys.Unauthorized),
        StatusCodes.Status403Forbidden => stringProvider.Get(DetailKeys.Unauthorized),
        StatusCodes.Status404NotFound => stringProvider.Get(DetailKeys.NotFound),
        _ => stringProvider.Get(DetailKeys.UnexpectedError)
    };

    /// <summary>
    /// Maps an HTTP status code to a machine-readable internal error code.
    /// </summary>
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
