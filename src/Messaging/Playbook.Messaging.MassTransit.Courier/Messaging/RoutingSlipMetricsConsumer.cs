using MassTransit;
using MassTransit.Courier.Contracts;

namespace Playbook.Messaging.MassTransit.Courier.Messaging;

/// <summary>
/// Observes and records the terminal states of routing slips to provide telemetry and audit logging.
/// This consumer subscribes to the MassTransit events published upon completion or failure of a Courier exchange.
/// </summary>
public class RoutingSlipMetricsConsumer(ILogger<RoutingSlipMetricsConsumer> logger) :
    IConsumer<RoutingSlipCompleted>,
    IConsumer<RoutingSlipFaulted>
{
    /// <summary>
    /// Processes the <see cref="RoutingSlipCompleted"/> event when a workflow finishes all activities successfully.
    /// </summary>
    /// <param name="context">The consume context containing execution duration and tracking metadata.</param>
    /// <returns>A task representing the completion of the log operation.</returns>
    public Task Consume(ConsumeContext<RoutingSlipCompleted> context)
    {
        // Log the total execution time to monitor performance trends across distributed steps
        logger.LogInformation("🏆 WORKFLOW SUCCESS: TrackingNumber {Id} finished in {Duration}ms",
            context.Message.TrackingNumber, context.Message.Duration.TotalMilliseconds);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Processes the <see cref="RoutingSlipFaulted"/> event when an activity fails and compensation is triggered or exhausted.
    /// </summary>
    /// <param name="context">The consume context containing exception details and the point of failure.</param>
    /// <returns>A task representing the completion of the error log operation.</returns>
    public Task Consume(ConsumeContext<RoutingSlipFaulted> context)
    {
        // Identify the specific activity that threw the exception to facilitate rapid debugging
        logger.LogError("❌ WORKFLOW FAILED: TrackingNumber {Id}. Failure at Activity: {Activity}",
            context.Message.TrackingNumber, context.Message.ActivityExceptions.FirstOrDefault()?.Name);

        return Task.CompletedTask;
    }
}
