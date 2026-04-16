using System.Linq.Expressions;

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

    // ✅ Compiled ONCE per TResponse type (static field per generic instantiation).
    // Expression.Convert resolves the implicit Error → ErrorOr<T> operator at
    // expression-compile time, so this is as safe as a direct C# cast.
    private static readonly Func<Error, TResponse> ToErrorResponse = BuildErrorFactory();

    private static Func<Error, TResponse> BuildErrorFactory()
    {
        var responseType = typeof(TResponse);

        if (!responseType.IsGenericType ||
            responseType.GetGenericTypeDefinition() != typeof(ErrorOr<>))
        {
            throw new InvalidOperationException(
                $"IdempotencyBehavior<{typeof(TRequest).Name}, {responseType.Name}>: " +
                $"TResponse must be ErrorOr<T>. Got '{responseType.Name}'. " +
                "Ensure the command handler returns ErrorOr<T>.");
        }

        // Builds the lambda: (Error error) => (ErrorOr<TInner>)error
        // which is identical to what the C# compiler emits for an explicit cast,
        // but verified at class-load time instead of per-call.
        var param = Expression.Parameter(typeof(Error), "error");
        var conversion = Expression.Convert(param, responseType);   // resolves implicit op
        return Expression.Lambda<Func<Error, TResponse>>(conversion, param).Compile();
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestId = request.RequestId;

        // 1. ATOMIC ATTEMPT – try to reserve this RequestId
        if (!await idempotencyService.TryReserveAsync(requestId, LockDuration, cancellationToken))
        {
            var cachedResponse =
                await idempotencyService.GetResponseAsync<TResponse>(requestId, cancellationToken);

            if (cachedResponse is not null)
            {
                LogIdempotencyHit(logger, requestId);
                return cachedResponse;
            }

            // Reserved but no stored response → in-flight on another thread/node
            LogIdempotencyConflict(logger, requestId);
            // ✅ No dynamic; uses the compiled factory
            return ToErrorResponse(Error.Conflict(
                code: "Idempotency.InProgress",
                description: "This request is currently being processed. Please wait."));
        }

        try
        {
            var response = await next();

            // Cache only when the response is not a transient/unexpected error
            if (!response.IsError || response.Errors == null || response.Errors.Count == 0 ||
                response.Errors.All(e => e.Type is not ErrorType.Unexpected and not ErrorType.Validation))
            {
                await idempotencyService.CompleteAsync(requestId, response, CacheDuration, cancellationToken);
            }

            return response;
        }
        catch (Exception ex)
        {
            LogIdempotencyError(logger, ex, requestId);
            await idempotencyService.ReleaseAsync(requestId, cancellationToken);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Idempotency hit for RequestId {RequestId}. Returning cached response.")]
    static partial void LogIdempotencyHit(ILogger logger, Guid requestId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Idempotency conflict for RequestId {RequestId}. Request is in progress.")]
    static partial void LogIdempotencyConflict(ILogger logger, Guid requestId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Idempotency error for RequestId {RequestId}. Releasing reservation.")]
    static partial void LogIdempotencyError(ILogger logger, Exception ex, Guid requestId);
}
