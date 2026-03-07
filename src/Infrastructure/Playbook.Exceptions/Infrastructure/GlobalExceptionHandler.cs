using System.Collections.ObjectModel;
using System.Text;

using Microsoft.AspNetCore.Diagnostics;

using Playbook.Exceptions.Abstraction;
using Playbook.Exceptions.Abstraction.Exceptions;
using Playbook.Exceptions.Constants;
using Playbook.Exceptions.Core;
using Playbook.Exceptions.Models;

namespace Playbook.Exceptions.Infrastructure;

/// <summary>
/// A high-performance, centralized exception handler implementing the .NET 8 <see cref="IExceptionHandler"/> interface.
/// This middleware intercepts all unhandled exceptions, performs polymorphic mapping via <see cref="IExceptionMapper"/>,
/// executes structured logging with PII redaction, and returns RFC 7807 compliant Problem Details.
/// </summary>
public sealed class GlobalExceptionHandler(
    IExceptionMapper mapper,
    ILocalizedStringProvider stringProvider,
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment env)
    : IExceptionHandler
{
    private const string _logTemplate = "{StatusCode} {HttpMethod} {HttpPath} | [Customer:{CustomerNumber}] [Trace:{TraceId}] | {ErrorCode} | {ErrorMessage} | Details: {ValidationDetails}";

    /// <summary>
    /// Attempts to handle the exception by transforming it into a standardized API response.
    /// </summary>
    /// <param name="httpContext">The current HTTP request context.</param>
    /// <param name="exception">The unhandled exception to process.</param>
    /// <param name="cancellationToken">A token to monitor for operation cancellation.</param>
    /// <returns>True if the exception was handled; otherwise, false.</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        try
        {
            var traceId = httpContext.TraceIdentifier;

            // 1. Resolve Mapping via established Visitor/Double-Dispatch
            // Checks if the exception supports self-mapping to avoid complex type checking in the handler.
            var details = exception is IMappableException mapable
                ? mapable.Map(mapper)
                : GetDefaultDetails();

            // 2. Optimized Logging
            // Dispatches log entry with severity levels tuned to the HTTP status code.
            LogWithCorrectSeverity(httpContext, exception, details, traceId);

            // 3. Build RFC 7807 Response using .NET 8 Target-Typed New
            var response = new ApiErrorResponse
            {
                Status = details.StatusCode,
                Title = details.Title,
                Detail = details.Detail,
                ErrorCode = details.Code,
                Instance = httpContext.Request.Path,
                TraceId = traceId,
                Errors = details.Extensions ?? ReadOnlyDictionary<string, string[]>.Empty,
                // Debug information is strictly restricted to development environments to prevent sensitive data leakage.
                Debug = env.IsDevelopment() ? CreateDebugDetails(exception) : null
            };

            // 4. Send Response
            httpContext.Response.StatusCode = details.StatusCode;
            httpContext.Response.Headers["X-Trace-Id"] = traceId;

            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

            return true;
        }
        catch (Exception secondaryException)
        {
            // Fail-safe mechanism to ensure the client receives a valid JSON response even if the handler fails.
            return await HandleSafeFailAsync(httpContext, secondaryException, cancellationToken);
        }
    }

    /// <summary>
    /// Performs structured logging with dynamic severity and scope data.
    /// </summary>
    private void LogWithCorrectSeverity(HttpContext context, Exception exception, ExceptionMappingResult details, string traceId)
    {
        var method = context.Request.Method;
        var path = context.Request.Path;
        var customerNumber = context.Request.Headers["X-Customer-Number"].FirstOrDefault() ?? "0";

        string validationSummary = "N/A";
        if (exception is ValidationException valEx)
        {
            validationSummary = SanitizeErrorsForLogging(valEx.Errors);
        }

        // Structured Logging: Data is passed as a dictionary for efficient indexing by log providers like Serilog.
        var scopeData = new Dictionary<string, object>
        {
            ["CustomerNumber"] = customerNumber,
            ["TraceId"] = traceId,
            ["StatusCode"] = details.StatusCode,
            ["ErrorCode"] = details.Code,
            ["ErrorMessage"] = details.Detail,
            ["HttpMethod"] = method,
            ["HttpPath"] = path,
            ["ValidationDetails"] = validationSummary
        };

        using (logger.BeginScope(scopeData))
        {
            if (details.StatusCode >= 500)
                logger.LogError(exception, _logTemplate, details.StatusCode, method, path, customerNumber, traceId, details.Code, details.Detail, validationSummary);
            else if (details.StatusCode is 401 or 403)
                logger.LogWarning(_logTemplate, details.StatusCode, method, path, customerNumber, traceId, details.Code, details.Detail, validationSummary);
            else
                logger.LogInformation(_logTemplate, details.StatusCode, method, path, customerNumber, traceId, details.Code, details.Detail, validationSummary);
        }
    }

    /// <summary>
    /// Serializes validation errors into a log-friendly string while redacting sensitive fields.
    /// </summary>
    private static string SanitizeErrorsForLogging(IReadOnlyDictionary<string, ValidationError[]> errors)
    {
        var sb = new StringBuilder();
        foreach (var (key, val) in errors)
        {
            sb.Append('[').Append(key).Append(": ");
            for (int i = 0; i < val.Length; i++)
            {
                var err = val[i];
                // PII Protection: Compares against SecurityConstants.SensitiveKeys to mask passwords or tokens.
                var displayVal = SecurityConstants.SensitiveKeys.Contains(key) ? "***" : (err.AttemptedValue ?? "null");
                sb.Append(err.Message).Append("(Val: ").Append(displayVal).Append(')');
                if (i < val.Length - 1) sb.Append(", ");
            }
            sb.Append("] ");
        }
        return sb.ToString().Trim();
    }

    /// <summary>
    /// Recursively creates a debug detail object containing stack traces for internal development use.
    /// </summary>
    private static DebugDetails CreateDebugDetails(Exception ex, int depth = 0)
    {
        const int maxDepth = 10;
        var inner = ex.InnerException is not null && depth < maxDepth
            ? CreateDebugDetails(ex.InnerException, depth + 1)
            : null;
        return new(ex.Message, ex.StackTrace, inner);
    }

    /// <summary>
    /// Provides a default mapping for unhandled exceptions that do not provide specific domain context.
    /// </summary>
    private ExceptionMappingResult GetDefaultDetails() => new(
        stringProvider.Get(TitleKeys.InternalServer),
        stringProvider.Get(DetailKeys.UnexpectedError),
        ErrorCodes.InternalServerError,
        StatusCodes.Status500InternalServerError);

    /// <summary>
    /// Final fallback handler to prevent a total application crash or an empty 500 response.
    /// </summary>
    private static async ValueTask<bool> HandleSafeFailAsync(HttpContext context, Exception secEx, CancellationToken ct)
    {
        context.Response.StatusCode = 500;

        await context.Response.WriteAsJsonAsync(new ApiErrorResponse
        {
            Status = 500,
            Title = "Critical Failure",
            Detail = "An unexpected error occurred while processing the error response.",
            ErrorCode = ErrorCodes.InternalServerError,
            TraceId = context.TraceIdentifier
        }, ct);

        return true;
    }
}
