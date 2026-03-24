using System.Diagnostics;

using Microsoft.AspNetCore.SignalR;

namespace Playbook.Messaging.SignalR.Infrastructure.Filters;

/// <summary>
/// A high-performance diagnostic filter for the SignalR pipeline that monitors execution latency.
/// Implements <see cref="IHubFilter"/> to provide low-overhead profiling of hub method calls.
/// </summary>
/// <remarks>
/// This filter uses <see cref="Stopwatch.GetTimestamp"/> and <see cref="Stopwatch.GetElapsedTime(long)"/> 
/// to ensure extremely accurate, non-allocating timing of asynchronous operations. 
/// It leverages Source-Generated logging for optimal performance.
/// </remarks>
public sealed partial class PerformanceHubFilter(ILogger<PerformanceHubFilter> logger) : IHubFilter
{
    /// <summary>
    /// The execution time limit (in milliseconds) before a warning is logged.
    /// In a low-latency environment, methods exceeding 5ms are flagged for investigation.
    /// </summary>
    private const long _thresholdMs = 5;

    /// <summary>
    /// Source-generated logging definition for reporting slow hub method execution.
    /// This reduces reflection and boxing overhead compared to standard string-based logging.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Slow Hub Method Execution: {MethodName} took {ElapsedMilliseconds}ms")]
    static partial void LogSlowExecution(ILogger logger, string methodName, long elapsedMilliseconds);

    /// <summary>
    /// Wraps the hub method invocation to measure and log execution duration.
    /// </summary>
    /// <param name="invocationContext">Context for the current hub method invocation.</param>
    /// <param name="next">The next step in the hub invocation pipeline.</param>
    /// <returns>The result of the hub method invocation.</returns>
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        // High-performance timestamping: Capture current tick count without allocating a Stopwatch object.
        var startTimestamp = Stopwatch.GetTimestamp();

        try
        {
            return await next(invocationContext);
        }
        finally
        {
            // Calculating elapsed time using the modern high-precision timing API.
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

            if (elapsed.TotalMilliseconds > _thresholdMs)
            {
                LogSlowExecution(logger, invocationContext.HubMethodName, (long)elapsed.TotalMilliseconds);
            }
        }
    }
}
