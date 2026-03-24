using System.Diagnostics;

using Microsoft.AspNetCore.SignalR;

namespace Playbook.Messaging.SignalR.Infrastructure.Filters;

public sealed partial class PerformanceHubFilter(ILogger<PerformanceHubFilter> logger) : IHubFilter
{
    private const long _thresholdMs = 5;

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Slow Hub Method Execution: {MethodName} took {ElapsedMilliseconds}ms")]
    static partial void LogSlowExecution(ILogger logger, string methodName, long elapsedMilliseconds);

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        // High-performance timestamping
        var startTimestamp = Stopwatch.GetTimestamp();

        try
        {
            return await next(invocationContext);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

            if (elapsed.TotalMilliseconds > _thresholdMs)
            {
                LogSlowExecution(logger, invocationContext.HubMethodName, (long)elapsed.TotalMilliseconds);
            }
        }
    }
}
