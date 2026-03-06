using Microsoft.AspNetCore.Diagnostics;
using Playbook.Exceptions.Abstraction;
using Playbook.Exceptions.Constants;
using Playbook.Exceptions.Core;
using Playbook.Exceptions.Models;

namespace Playbook.Exceptions.Infrastructure;
public sealed class GlobalExceptionHandler(
    IEnumerable<IExceptionMapper> mappers,
    ILocalizedStringProvider stringProvider,
    ILogger<GlobalExceptionHandler> logger,
    IHttpContextAccessor httpContextAccessor,
    IHostEnvironment env)
    : IExceptionHandler
{
    const string LogTemplate =
        "{StatusCode} {HttpMethod} {HttpPath} | " +
        "[Customer:{CustomerNumber}] [Trace:{TraceId}] | " +
        "{ErrorCode} | {ErrorMessage}";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        try
        {
            var traceId = httpContext.TraceIdentifier;

            // 1. Resolve Mapping
            var mapper = mappers.FirstOrDefault(x => x.CanMap(exception));
            var details = mapper?.Map(exception) ?? GetDefaultDetails();

            // 2. Logging based on Severity logic
            LogWithCorrectSeverity(exception, details, traceId);

            // 3. Build RFC 7807 Response
            var response = new ApiErrorResponse
            {
                Status = details.StatusCode,
                Title = details.Title,
                Detail = details.Detail,
                ErrorCode = details.ErrorCode,
                Instance = httpContext.Request.Path,
                TraceId = traceId,
                Debug = env.IsDevelopment() ? CreateDebugDetails(exception) : null
            };

            if (details.ValidationErrors is not null)
            {
                response.Errors = new Dictionary<string, string[]>();

                foreach (var error in details.ValidationErrors)
                {
                    // ValidationProblemDetails.Errors is initialized by the base class
                    response.Errors.TryAdd(error.Key, error.Value);
                }
            }

            // 4. Send Response
            httpContext.Response.StatusCode = details.StatusCode;
            httpContext.Response.Headers["X-Trace-Id"] = traceId;

            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

            return true;
        }
        catch (Exception secondaryException)
        {
            // CRITICAL: The handler itself failed. Execute Safe-Fail protocol.
            return await HandleSafeFailAsync(httpContext, secondaryException, cancellationToken);
        }
    }

    private void LogWithCorrectSeverity(Exception exception, ExceptionMappingResult details, string traceId)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var method = httpContext?.Request.Method ?? "N/A";
        var path = httpContext?.Request.Path ?? "N/A";

        // Dynamically retrieve customer number if available
        var customerNumber = httpContext?.Request.Headers["X-Customer-Number"].FirstOrDefault() ?? "0";

        // 1. The Scope: Every log inside this block gets these properties for observability
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CustomerNumber"] = customerNumber,
            ["TraceId"] = traceId,
            ["StatusCode"] = details.StatusCode,
            ["ErrorCode"] = details.ErrorCode,
            ["HttpMethod"] = method,
            ["HttpPath"] = path
        }))
        {
            var args = new object[]
            {
                details.StatusCode,
                method,
                path,
                customerNumber,
                traceId,
                details.ErrorCode,
                exception.Message
            };

            if (details.StatusCode >= 500)
                logger.LogError(exception, LogTemplate, args);
            else if (details.StatusCode is 401 or 403)
                logger.LogWarning(LogTemplate, args);
            else
                logger.LogInformation(LogTemplate, args);
        }
    }

    private static DebugDetails CreateDebugDetails(Exception ex) =>
        new(ex.Message,
            ex.StackTrace,
            ex.InnerException is not null ? CreateDebugDetails(ex.InnerException) : null);

    private ExceptionMappingResult GetDefaultDetails() => new(
        StatusCodes.Status500InternalServerError,
        stringProvider.Get(TitleKeys.InternalServer),
        stringProvider.Get(DetailKeys.UnexpectedError),
        ErrorCodes.InternalServerError,
        null);

    private static async ValueTask<bool> HandleSafeFailAsync(
        HttpContext httpContext,
        Exception secondaryException,
        CancellationToken ct)
    {
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/json";

        var fallback = new
        {
            Status = 500,
            Title = "Critical Failure",
            Detail = "The error handling pipeline crashed. Check server logs.",
            TraceId = httpContext.TraceIdentifier
        };

        await httpContext.Response.WriteAsJsonAsync(fallback, ct);
        return true;
    }
}