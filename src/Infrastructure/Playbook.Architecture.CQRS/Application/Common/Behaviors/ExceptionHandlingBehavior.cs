using ErrorOr;

using MediatR;

namespace Playbook.Architecture.CQRS.Application.Common.Behaviors;

public class ExceptionHandlingBehavior<TRequest, TResponse>(ILogger<TRequest> logger)
: IPipelineBehavior<TRequest, TResponse>
where TRequest : IRequest<TResponse>
where TResponse : IErrorOr
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {RequestName}", typeof(TRequest).Name);

            // Using dynamic to return an ErrorOr containing the unexpected error
            return (dynamic)Error.Unexpected("Server.Error", "An unhandled error occurred.");
        }
    }
}
