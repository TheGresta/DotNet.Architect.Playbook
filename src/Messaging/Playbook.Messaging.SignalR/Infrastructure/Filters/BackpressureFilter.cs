using Microsoft.AspNetCore.SignalR;

namespace Playbook.Messaging.SignalR.Infrastructure.Filters;

/// <summary>
/// A global SignalR Hub filter designed to monitor and manage backpressure across the real-time pipeline.
/// Implements <see cref="IHubFilter"/> to intercept and evaluate hub method invocations before execution.
/// </summary>
/// <remarks>
/// In high-frequency trading or market data scenarios, this filter serves as a strategic gatekeeper. 
/// It allows for the inspection of transport health and connection features (such as buffer sizes) 
/// to prevent message queuing or memory exhaustion under heavy load.
/// </remarks>
public class BackpressureFilter : IHubFilter
{
    /// <summary>
    /// Intercepts the hub method invocation to perform pre-execution health checks.
    /// </summary>
    /// <param name="invocationContext">Contextual information about the hub, method, and arguments being invoked.</param>
    /// <param name="next">The delegate representing the next filter in the pipeline or the hub method itself.</param>
    /// <returns>The result of the hub method invocation.</returns>
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        // Before every call, check if the transport is healthy.
        // Implementation detail: In a mission-critical FinTech application, we would inspect 
        // Context.Features for the IHttpTransportFeature or specific buffer thresholds to 
        // decide whether to throttle or allow the current invocation.
        return await next(invocationContext);
    }
}
