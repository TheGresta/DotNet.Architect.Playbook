using System.Diagnostics;

using ErrorOr;

using MediatR;

namespace Playbook.Architecture.CQRS.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<TRequest> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IErrorOr
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;

        logger.LogInformation("Processing request {RequestName}", requestName);

        long startTimestamp = Stopwatch.GetTimestamp();

        var response = await next(cancellationToken);

        TimeSpan elapsed = Stopwatch.GetElapsedTime(startTimestamp);

        if (response.IsError)
        {
            logger.LogWarning(
                "Request {RequestName} failed in {Elapsed:0.000}ms with {ErrorCount} errors. Errors: {ErrorCodes}",
                requestName,
                elapsed.TotalMilliseconds,
                response.Errors?.Count ?? 0,
                string.Join(", ", response.Errors?.Select(e => e.Code) ?? []));
        }
        else
        {
            logger.LogInformation(
                "Request {RequestName} succeeded in {Elapsed:0.000}ms",
                requestName,
                elapsed.TotalMilliseconds);
        }

        return response;
    }
}
