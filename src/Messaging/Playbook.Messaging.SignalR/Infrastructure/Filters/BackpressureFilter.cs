using Microsoft.AspNetCore.SignalR;

namespace Playbook.Messaging.SignalR.Infrastructure.Filters;

public class BackpressureFilter : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        // Before every call, check if the transport is healthy
        // In a real FinTech app, we'd check the Context.Features for buffer size
        return await next(invocationContext);
    }
}
