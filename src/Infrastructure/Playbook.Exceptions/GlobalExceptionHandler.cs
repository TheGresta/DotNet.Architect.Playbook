using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Localization;
using Playbook.Exceptions.Constants;
using Playbook.Exceptions.Mapping;
using Playbook.Exceptions.Resources;

namespace Playbook.Exceptions;
public sealed class GlobalExceptionHandler(
    IEnumerable<IExceptionMapper> mappers,
    IStringLocalizer<SharedResources> localizer,
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment env)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        try
        {
            var traceId = httpContext.TraceIdentifier;

            // Find a mapper that supports this exception, or fallback to default
            var mapper = mappers.FirstOrDefault(x => x.CanMap(exception));
            var details = mapper?.Map(exception) ?? GetDefaultDetails();

            LogWithCorrectSeverity(exception, details.StatusCode, traceId);

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

            httpContext.Response.StatusCode = details.StatusCode;
            httpContext.Response.Headers["X-Trace-Id"] = traceId;

            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

            return true;
        }
        catch (Exception secondaryException)
        {
            // --- THE SAFE-FAIL FALLBACK ---
            // If we are here, our error handler CRASHED. 
            // We do NOT use any injected services here (no logger, no localizer).
            return await HandleSafeFailAsync(httpContext, secondaryException, cancellationToken);
        }
    }

    private void LogWithCorrectSeverity(Exception exception, int statusCode, string traceId)
    {
        const string messageTemplate = "An error occurred [TraceId: {TraceId}]: {Message}";

        if (statusCode >= 500)
        {
            // Actual server failures need immediate attention
            logger.LogError(exception, messageTemplate, traceId, exception.Message);
        }
        else if (statusCode == StatusCodes.Status401Unauthorized || statusCode == StatusCodes.Status403Forbidden)
        {
            // Security-related events are important but expected
            logger.LogWarning(messageTemplate, traceId, exception.Message);
        }
        else
        {
            // 400s and 404s are just part of regular API traffic
            logger.LogInformation(messageTemplate, traceId, exception.Message);
        }
    }

    private static DebugDetails CreateDebugDetails(Exception ex) =>
        new(ex.Message,
            ex.StackTrace,
            ex.InnerException is not null ? CreateDebugDetails(ex.InnerException) : null);

    private ExceptionMappingResult GetDefaultDetails() => new(
        StatusCodes.Status500InternalServerError,
        localizer[LocalizationKeys.InternalServerTitle],
        localizer[LocalizationKeys.InternalServerTitle],
        ErrorCodes.InternalServerError,
        null);

    private static async ValueTask<bool> HandleSafeFailAsync(
        HttpContext httpContext,
        Exception originalError,
        CancellationToken ct)
    {
        // Use a hardcoded 500 status
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/json";

        // Create a minimal JSON string manually or via a simple static object
        // We avoid complex logic here to ensure this cannot fail.
        var fallbackResponse = new
        {
            Status = 500,
            Title = ErrorCodes.InternalServerError,
            Detail = "A critical failure occurred in the error handling pipeline.",
            TraceId = httpContext.TraceIdentifier
        };

        await httpContext.Response.WriteAsJsonAsync(fallbackResponse, ct);

        return true;
    }
}