using MassTransit;
using MassTransit.Courier.Contracts;

namespace Playbook.Messaging.MassTransit.Courier.Messaging;

public class RoutingSlipMetricsConsumer(ILogger<RoutingSlipMetricsConsumer> logger) :
    IConsumer<RoutingSlipCompleted>,
    IConsumer<RoutingSlipFaulted>
{
    public Task Consume(ConsumeContext<RoutingSlipCompleted> context)
    {
        logger.LogInformation("🏆 WORKFLOW SUCCESS: TrackingNumber {Id} finished in {Duration}ms",
            context.Message.TrackingNumber, context.Message.Duration.TotalMilliseconds);
        return Task.CompletedTask;
    }

    public Task Consume(ConsumeContext<RoutingSlipFaulted> context)
    {
        logger.LogError("❌ WORKFLOW FAILED: TrackingNumber {Id}. Failure at Activity: {Activity}",
            context.Message.TrackingNumber, context.Message.ActivityExceptions.FirstOrDefault()?.Name);
        return Task.CompletedTask;
    }
}
