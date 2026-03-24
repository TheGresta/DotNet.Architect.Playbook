using ErrorOr;

using MediatR;

using Playbook.Architecture.CQRS.Application.Common.Interfaces;

namespace Playbook.Architecture.CQRS.Application.Common.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse>(
    // IDbContextTransaction or IUnitOfWork would be injected here
    ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
    where TResponse : IErrorOr
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        return await next();

        //var strategy = dbContext.Database.CreateExecutionStrategy();

        //return await strategy.ExecuteAsync(async () =>
        //{
        //    dbContext.ChangeTracker.Clear();
        //    // Use a higher isolation level if your domain requires strict consistency
        //    await using var transaction = await dbContext.Database.BeginTransactionAsync(
        //        IsolationLevel.ReadCommitted,
        //        cancellationToken);

        //    try
        //    {
        //        var response = await next();

        //        if (response.IsError)
        //        {
        //            logger.LogWarning("Transaction rollback: Domain Error in {CommandName}", typeof(TRequest).Name);
        //            await transaction.RollbackAsync(cancellationToken);
        //            return response;
        //        }

        //        await dbContext.SaveChangesAsync(cancellationToken);
        //        await transaction.CommitAsync(cancellationToken);

        //        return response;
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.LogError(ex, "Transaction failed: Forced rollback for {CommandName}", typeof(TRequest).Name);

        //        // Rollback without the request's CT to ensure it finishes even if the user disconnected
        //        await transaction.RollbackAsync(CancellationToken.None);
        //        throw;
        //    }
        //});
    }
}
