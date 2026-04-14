using System.Diagnostics;

using ErrorOr;

using MediatR;

using Microsoft.Extensions.Logging;

using Playbook.Security.IdP.Application.Abstractions.Messaging;
using Playbook.Security.IdP.Application.Abstractions.Security;

namespace Playbook.Security.IdP.Application.Behaviors;

public sealed partial class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    IRequestContext context)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IErrorOr
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        // Structured scope for centralized indexing in Loki/Grafana
        var logScope = new Dictionary<string, object>(7)
        {
            ["CorrelationId"] = context.CorrelationId,
            ["UserId"] = context.UserId?.ToString() ?? "Anonymous",
            ["IpAddress"] = context.IpAddress,
            ["DeviceId"] = context.DeviceId ?? "Unknown",
            ["RequestType"] = requestName,
            ["Version"] = "1.0"
        };

        if (request is IIdempotentCommand idempotent)
            logScope["IdempotencyKey"] = idempotent.RequestId;

        using (logger.BeginScope(logScope))
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var response = await next();
                stopwatch.Stop();

                if (response.IsError)
                {
                    LogFailure(requestName, stopwatch.ElapsedMilliseconds, response.Errors);
                }
                else if (request is ICommand || request is IAuditableRequest)
                {
                    // Success log for state-changing operations
                    LogSuccess(logger, requestName, stopwatch.ElapsedMilliseconds);
                }

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogCritical(logger, ex, requestName, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }

    private void LogFailure(string requestName, long elapsedMs, List<Error>? errors)
    {
        if (errors is null || errors.Count == 0) return;

        var errorData = errors.Select(e => new { e.Code, e.Type });

        if (errors.Any(e => e.Type == ErrorType.Unexpected))
            LogUnexpectedError(logger, requestName, elapsedMs, errorData);
        else if (errors.All(e => e.Type == ErrorType.Validation))
            LogValidationError(logger, requestName, elapsedMs, errorData);
        else
            LogWarningError(logger, requestName, elapsedMs, errorData);
    }

    // --- High-Performance Logger Message Definitions (Source Generated) ---

    [LoggerMessage(Level = LogLevel.Information, Message = "[{RequestName}] Success in {ElapsedMs}ms.")]
    static partial void LogSuccess(ILogger logger, string requestName, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Critical, Message = "[{RequestName}] FATAL EXCEPTION after {ElapsedMs}ms.")]
    static partial void LogCritical(ILogger logger, Exception ex, string requestName, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Error, Message = "[{RequestName}] Unexpected failure ({ElapsedMs}ms): {@Errors}")]
    static partial void LogUnexpectedError(ILogger logger, string requestName, long elapsedMs, object errors);

    [LoggerMessage(Level = LogLevel.Information, Message = "[{RequestName}] Validation failed ({ElapsedMs}ms): {@Errors}")]
    static partial void LogValidationError(ILogger logger, string requestName, long elapsedMs, object errors);

    [LoggerMessage(Level = LogLevel.Warning, Message = "[{RequestName}] Business rule violation ({ElapsedMs}ms): {@Errors}")]
    static partial void LogWarningError(ILogger logger, string requestName, long elapsedMs, object errors);
}
