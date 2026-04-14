using ErrorOr;

using MediatR;

using Microsoft.Extensions.Logging;

using Playbook.Security.IdP.Application.Abstractions.Messaging;
using Playbook.Security.IdP.Application.Abstractions.Security;

namespace Playbook.Security.IdP.Application.Behaviors;

public sealed partial class IdempotencyBehavior<TRequest, TResponse>(
    IIdempotencyService idempotencyService,
    ILogger<IdempotencyBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IIdempotentCommand
    where TResponse : IErrorOr
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(1);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestId = request.RequestId;

        // 1. ATOMIC ATTEMPT: Try to reserve this RequestId
        // If this returns false, the ID is either 'In Progress' or 'Completed'
        if (!await idempotencyService.TryReserveAsync(requestId, LockDuration))
        {
            var cachedResponse = await idempotencyService.GetResponseAsync<TResponse>(requestId);

            if (cachedResponse is not null)
            {
                LogIdempotencyHit(logger, requestId);
                return cachedResponse;
            }

            // If it's reserved but has no response, it's currently being processed by another thread/node
            LogIdempotencyConflict(logger, requestId);
            return (TResponse)(dynamic)Error.Conflict(
                code: "Idempotency.InProgress",
                description: "This request is currently being processed. Please wait.");
        }

        try
        {
            // 2. Execute the downstream pipeline
            var response = await next();

            // 3. Gold Standard: We ONLY cache successful or valid business logic results.
            // Critical system failures (Exceptions) shouldn't be cached so they can be retried.
            if (!response.IsError || IsBusinessFailure(response.Errors ?? []))
            {
                await idempotencyService.CompleteAsync(requestId, response, CacheDuration);
            }

            return response;
        }
        catch (Exception ex)
        {
            // If the handler crashes, we MUST release the reservation so the user can retry.
            LogIdempotencyError(logger, ex, requestId);
            await idempotencyService.ReleaseAsync(requestId);
            throw;
        }
    }

    private static bool IsBusinessFailure(List<Error> errors) =>
        errors.All(e => e.Type != ErrorType.Unexpected && e.Type != ErrorType.Validation);

    // --- High-Performance Logging ---

    [LoggerMessage(Level = LogLevel.Information, Message = "Idempotency hit for RequestId: {RequestId}. Returning cached response.")]
    static partial void LogIdempotencyHit(ILogger logger, Guid requestId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Idempotency conflict for RequestId: {RequestId}. Request is already in progress.")]
    static partial void LogIdempotencyConflict(ILogger logger, Guid requestId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error during idempotent execution for RequestId: {RequestId}.")]
    static partial void LogIdempotencyError(ILogger logger, Exception ex, Guid requestId);
}
