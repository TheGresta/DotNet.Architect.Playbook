using ErrorOr;

using MediatR;

using Playbook.Architecture.CQRS.Application.Common.Interfaces;

namespace Playbook.Architecture.CQRS.Application.Common.Behaviors;

public class TransactionBehavior<TRequest, TResponse>(
    ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
    where TResponse : IErrorOr
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        logger.LogInformation("--> Starting transaction for {CommandName}", typeof(TRequest).Name);

        // In a real app, you would use your DbContext or UnitOfWork here
        //using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        try
        {
            var response = await next(ct);

            if (!response.IsError)
            {
                //await unitOfWork.SaveChangesAsync(ct);
                //await transaction.CommitAsync(ct);
                logger.LogInformation("--> Transaction committed for {CommandName}", typeof(TRequest).Name);
            }
            else
            {
                // ErrorOr logic: If there's a domain error, we roll back 
                // because we don't want partial state changes.
                //await transaction.RollbackAsync(ct);
                logger.LogWarning("--> Transaction rolled back due to Domain Error in {CommandName}", typeof(TRequest).Name);
            }

            return response;
        }
        catch (Exception ex)
        {
            //await transaction.RollbackAsync(ct);
            logger.LogError(ex, "--> Transaction rolled back due to Exception in {CommandName}", typeof(TRequest).Name);
            throw; // Re-throw so ExceptionHandlingBehavior can catch it
        }
    }
}
