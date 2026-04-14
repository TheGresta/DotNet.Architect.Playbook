using ErrorOr;

using MediatR;

using Microsoft.Extensions.Logging;

using Playbook.Security.IdP.Application.Abstractions.Messaging;
using Playbook.Security.IdP.Domain.Common;

namespace Playbook.Security.IdP.Application.Behaviors;

public sealed partial class TransactionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    IDomainEventDispatcher dispatcher,
    ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ITransactionalRequest
    where TResponse : IErrorOr
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);

            var response = await next();

            // 1. If the Handler returned a business error (ErrorOr.Failure)
            if (response.IsError)
            {
                LogRollback(logger, requestName, "Business logic failure");
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return response;
            }

            // 2. Dispatch Domain Events BEFORE committing.
            // If an event handler fails (e.g., trying to assign a role that doesn't exist),
            // it will throw an exception and trigger the catch block (Rollback).
            await dispatcher.DispatchEventsAsync(cancellationToken);

            // 3. Final Persistence
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // 4. Seal the deal
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            try
            {
                // 5. Fatal catch-all: If DB snaps or an Event Handler crashes
                LogFatal(logger, ex, requestName);
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
            }
            catch (Exception rollbackEx)
            {
                throw new AggregateException(ex, rollbackEx);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Transaction for {RequestName} rolled back due to {Reason}.")]
    static partial void LogRollback(ILogger logger, string requestName, string reason);

    [LoggerMessage(Level = LogLevel.Critical, Message = "Transaction for {RequestName} FAILED unexpectedly. Rolling back state.")]
    static partial void LogFatal(ILogger logger, Exception ex, string requestName);
}
