using System.Diagnostics;

using ErrorOr;

using MediatR;

namespace Playbook.Architecture.CQRS.Application.Common.Behaviors;

/// <summary>
/// A high-performance diagnostic pipeline behavior that provides structured logging for all MediatR requests.
/// It tracks execution timing using high-resolution timestamps and distinguishes between successful and failed operations
/// to facilitate efficient observability and troubleshooting.
/// </summary>
/// <typeparam name="TRequest">The type of the request being handled, constrained to <see cref="IRequest{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The type of the response, constrained to <see cref="IErrorOr"/> for structured error reporting.</typeparam>
/// <param name="logger">The logger instance used for telemetry, typically scoped to the request type.</param>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<TRequest> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IErrorOr
{
    /// <summary>
    /// Executes the logging logic around the request lifecycle.
    /// Captures start time, success status, elapsed duration, and specific error codes if the operation fails.
    /// </summary>
    /// <param name="request">The incoming request object.</param>
    /// <param name="next">The execution delegate for the next step in the pipeline.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <typeparamref name="TResponse"/> containing the operation results.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;

        logger.LogInformation("Processing request {RequestName}", requestName);

        // Utilize high-resolution Stopwatch timestamps to minimize allocation overhead compared to DateTime.Now.
        long startTimestamp = Stopwatch.GetTimestamp();

        var response = await next(cancellationToken);

        // Calculate the precise duration of the request execution across the rest of the pipeline.
        TimeSpan elapsed = Stopwatch.GetElapsedTime(startTimestamp);

        if (response.IsError)
        {
            // Log at Warning level for logical failures (e.g., validation or business rule violations).
            // Includes the count and specific codes of the errors returned for rapid debugging.
            logger.LogWarning(
                "Request {RequestName} failed in {Elapsed:0.000}ms with {ErrorCount} errors. Errors: {ErrorCodes}",
                requestName,
                elapsed.TotalMilliseconds,
                response.Errors?.Count ?? 0,
                string.Join(", ", response.Errors?.Select(e => e.Code) ?? []));
        }
        else
        {
            // Log successful completion with performance metrics.
            logger.LogInformation(
                "Request {RequestName} succeeded in {Elapsed:0.000}ms",
                requestName,
                elapsed.TotalMilliseconds);
        }

        return response;
    }
}
