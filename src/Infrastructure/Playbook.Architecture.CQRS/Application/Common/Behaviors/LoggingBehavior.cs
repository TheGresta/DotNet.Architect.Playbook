using System.Diagnostics;

using ErrorOr;

using MediatR;

namespace Playbook.Architecture.CQRS.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(ILogger<TRequest> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IErrorOr
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        logger.LogInformation("Starting request {RequestName}", typeof(TRequest).Name);
        var timer = Stopwatch.StartNew();

        var response = await next(ct);

        timer.Stop();
        if (response.IsError)
        {
            logger.LogWarning("Request {RequestName} failed in {Elapsed}ms with {ErrorCount} errors",
                typeof(TRequest).Name, timer.ElapsedMilliseconds, response.Errors?.Count);
        }
        else
        {
            logger.LogInformation("Request {RequestName} completed in {Elapsed}ms",
                typeof(TRequest).Name, timer.ElapsedMilliseconds);
        }

        return response;
    }
}
