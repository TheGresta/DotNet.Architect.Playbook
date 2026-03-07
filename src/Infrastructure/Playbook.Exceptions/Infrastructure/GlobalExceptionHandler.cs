using System.Collections.ObjectModel;
using System.Text;

using Microsoft.AspNetCore.Diagnostics;

using Playbook.Exceptions.Abstraction;
using Playbook.Exceptions.Abstraction.Exceptions;
using Playbook.Exceptions.Constants;
using Playbook.Exceptions.Core;
using Playbook.Exceptions.Models;

namespace Playbook.Exceptions.Infrastructure;

public sealed class GlobalExceptionHandler(
    IExceptionMapper mapper,
    ILocalizedStringProvider stringProvider,
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment env)
    : IExceptionHandler
{
    private const string _logTemplate = "{StatusCode} {HttpMethod} {HttpPath} | [Customer:{CustomerNumber}] [Trace:{TraceId}] | {ErrorCode} | {ErrorMessage} | Details: {ValidationDetails}";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        try
        {
            var traceId = httpContext.TraceIdentifier;

            // 1. Resolve Mapping via established Visitor/Double-Dispatch
            var details = exception is IMapableException mapable
                ? mapable.Map(mapper)
                : GetDefaultDetails();

            // 2. Optimized Logging
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
            return await HandleSafeFailAsync(httpContext, secondaryException, cancellationToken);
        }
    }

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

        // Structured Logging: Avoid string concatenation in the message template
        var scopeData = new Dictionary<string, object>
        {
            ["CustomerNumber"] = customerNumber,
            ["TraceId"] = traceId,
            ["StatusCode"] = details.StatusCode,
            ["ErrorCode"] = details.Code,
            ["HttpMethod"] = method,
            ["HttpPath"] = path,
            ["ValidationDetails"] = validationSummary
        };

        using (logger.BeginScope(scopeData))
        {
            if (details.StatusCode >= 500)
                logger.LogError(exception, _logTemplate, details.StatusCode, method, path, customerNumber, traceId, details.Code, exception.Message, validationSummary);
            else if (details.StatusCode is 401 or 403)
                logger.LogWarning(_logTemplate, details.StatusCode, method, path, customerNumber, traceId, details.Code, exception.Message, validationSummary);
            else
                logger.LogInformation(_logTemplate, details.StatusCode, method, path, customerNumber, traceId, details.Code, exception.Message, validationSummary);
        }
    }

    private static string SanitizeErrorsForLogging(IReadOnlyDictionary<string, ValidationError[]> errors)
    {
        var sb = new StringBuilder();
        foreach (var (key, val) in errors)
        {
            sb.Append('[').Append(key).Append(": ");
            for (int i = 0; i < val.Length; i++)
            {
                var err = val[i];
                var displayVal = SecurityConstants.SensitiveKeys.Contains(key) ? "***" : (err.AttemptedValue ?? "null");
                sb.Append(err.Message).Append("(Val: ").Append(displayVal).Append(')');
                if (i < val.Length - 1) sb.Append(", ");
            }
            sb.Append("] ");
        }
        return sb.ToString().Trim();
    }

    private static DebugDetails CreateDebugDetails(Exception ex) =>
        new(ex.Message, ex.StackTrace, ex.InnerException is not null ? CreateDebugDetails(ex.InnerException) : null);

    private ExceptionMappingResult GetDefaultDetails() => new(
        stringProvider.Get(TitleKeys.InternalServer),
        stringProvider.Get(DetailKeys.UnexpectedError),
        ErrorCodes.InternalServerError,
        StatusCodes.Status500InternalServerError);

    private static async ValueTask<bool> HandleSafeFailAsync(HttpContext context, Exception secEx, CancellationToken ct)
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { Status = 500, Title = "Critical Failure", TraceId = context.TraceIdentifier }, ct);
        return true;
    }
}
