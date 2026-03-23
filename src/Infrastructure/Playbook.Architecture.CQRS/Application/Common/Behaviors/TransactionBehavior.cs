using ErrorOr;

using MediatR;

namespace Playbook.Architecture.CQRS.Application.Common.Behaviors;

public class TransactionBehavior<TRequest, TResponse>(ILogger<TRequest> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IErrorOr
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        // Only wrap Commands in a transaction
        if (typeof(TRequest).Name.EndsWith("Query")) return await next(ct);

        logger.LogInformation("--> Open Transaction for {RequestName}", typeof(TRequest).Name);

        var response = await next(ct);

        if (!response.IsError)
        {
            logger.LogInformation("--> Commit Transaction for {RequestName}", typeof(TRequest).Name);
        }
        else
        {
            logger.LogWarning("--> Rollback Transaction for {RequestName}", typeof(TRequest).Name);
        }

        return response;
    }
}
