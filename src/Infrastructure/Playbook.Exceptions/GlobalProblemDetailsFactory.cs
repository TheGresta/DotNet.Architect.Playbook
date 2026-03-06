using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Playbook.Exceptions.Constants;
using Playbook.Exceptions.Localization;

namespace Playbook.Exceptions;

public class GlobalProblemDetailsFactory(
    ILocalizedStringProvider stringProvider,
    IHostEnvironment env) : ProblemDetailsFactory
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

        // 1. Map the Title
        var finalTitle = title ?? status switch
        {
            StatusCodes.Status401Unauthorized => stringProvider.Get(TitleKeys.Unauthorized),
            StatusCodes.Status403Forbidden => stringProvider.Get(TitleKeys.Unauthorized),
            StatusCodes.Status422UnprocessableEntity => stringProvider.Get(TitleKeys.BusinessRule),
            _ => stringProvider.Get(TitleKeys.InternalServer)
        };

        // 2. Map the Machine Error Code
        var errorCode = status switch
        {
            StatusCodes.Status401Unauthorized => ErrorCodes.Unauthorized,
            StatusCodes.Status422UnprocessableEntity => ErrorCodes.BusinessRuleViolation,
            StatusCodes.Status400BadRequest => ErrorCodes.ValidationError,
            _ => ErrorCodes.InternalServerError
        };

        return new ApiErrorResponse
        {
            Status = status,
            Title = finalTitle,
            // If framework didn't provide detail, use our generic unexpected error detail
            Detail = detail ?? stringProvider.Get(DetailKeys.UnexpectedError),
            Instance = instance ?? httpContext.Request.Path,
            TraceId = httpContext.TraceIdentifier,
            ErrorCode = errorCode
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
            Title = title ?? stringProvider.Get(TitleKeys.ValidationError),
            Detail = detail ?? stringProvider.Get(DetailKeys.ValidationSummary),
            Instance = instance ?? httpContext.Request.Path,
            TraceId = httpContext.TraceIdentifier,
            ErrorCode = ErrorCodes.ValidationError
        };

        // Map framework-generated ModelState errors (e.g., from [Required] or [EmailAddress])
        foreach (var entry in modelStateDictionary)
        {
            if (entry.Value.Errors.Count > 0)
            {
                // We try to localize the error message if the framework provided a Key,
                // otherwise we use the raw message provided by .NET.
                var errorMessages = entry.Value.Errors
                    .Select(e => stringProvider.Get(e.ErrorMessage))
                    .ToArray();

                problemDetails.Errors.Add(entry.Key, errorMessages);
            }
        }

        return problemDetails;
    }
}