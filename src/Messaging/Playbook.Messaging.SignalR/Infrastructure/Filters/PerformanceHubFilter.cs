using System.Diagnostics;

using Microsoft.AspNetCore.SignalR;

namespace Playbook.Messaging.SignalR.Infrastructure.Filters;

public sealed partial class PerformanceHubFilter(ILogger<PerformanceHubFilter> logger) : IHubFilter
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Hub Method {MethodName} executed in {ElapsedMilliseconds}ms")]
    static partial void LogExecutionTime(ILogger logger, string methodName, long elapsedMilliseconds);

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            return await next(invocationContext);
        }
        finally
        {
            sw.Stop();
            // Only log if it's suspiciously slow for our HFT standards (> 5ms)
            if (sw.ElapsedMilliseconds > 5)
            {
                LogExecutionTime(logger, invocationContext.HubMethodName, sw.ElapsedMilliseconds);
            }
        }
    }
}
