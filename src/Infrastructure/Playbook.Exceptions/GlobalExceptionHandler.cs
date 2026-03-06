using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Localization;
using Playbook.Exceptions.Constants;
using Playbook.Exceptions.Localization;
using Playbook.Exceptions.Mapping;

namespace Playbook.Exceptions;
public sealed class GlobalExceptionHandler(
    IEnumerable<IExceptionMapper> mappers,
    ILocalizedStringProvider stringProvider, // Swapped for our smart provider
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

            // 1. Resolve Mapping
            var mapper = mappers.FirstOrDefault(x => x.CanMap(exception));
            var details = mapper?.Map(exception) ?? GetDefaultDetails();

            // 2. Logging based on Severity logic
            LogWithCorrectSeverity(exception, details.StatusCode, traceId);

            // 3. Build RFC 7807 Response
            var response = new ApiErrorResponse
            {
                Status = details.StatusCode,
                Title = details.Title,
                Detail = details.Detail,
                ErrorCode = details.ErrorCode,
                Instance = httpContext.Request.Path,
                TraceId = traceId,
                Debug = null//env.IsDevelopment() ? CreateDebugDetails(exception) : null
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

    private void LogWithCorrectSeverity(Exception exception, int statusCode, string traceId)
    {
        const string template = "Exception occurred [TraceId: {TraceId}]: {Message}";

        if (statusCode >= 500)
            logger.LogError(exception, template, traceId, exception.Message);
        else if (statusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
            logger.LogWarning(template, traceId, exception.Message);
        else
            logger.LogInformation(template, traceId, exception.Message);
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