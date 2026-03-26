using MassTransit;

using Playbook.Messaging.MassTransit.Application.Consumers;
using Playbook.Messaging.MassTransit.Domain;
using Playbook.Messaging.MassTransit.Infrastructure.Persistence;

namespace Playbook.Messaging.MassTransit.Infrastructure.Messaging;

public static class MessagingRegistration
{
    public static IServiceCollection AddEnterpriseMessaging(this IServiceCollection services, IConfiguration config)
    {
        var busOptions = config.GetSection(MessageBusOptions.SectionName).Get<MessageBusOptions>()
                         ?? throw new InvalidOperationException("Messaging config missing.");

        services.AddMassTransit(x =>
        {
            x.AddConsumers(typeof(Program).Assembly);

            x.AddSagaStateMachine<WorkflowStateMachine, WorkflowState>()
                .EntityFrameworkRepository(r =>
                {
                    r.ExistingDbContext<AppDbContext>();
                    r.UsePostgres();
                });

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(busOptions.Host, "/", h =>
                {
                    h.Username(busOptions.Username);
                    h.Password(busOptions.Password);
                });

                // 1. Configure the "High-Risk" Undo Endpoints with Defensive Middleware
                // We use explicit naming to ensure these critical paths are easily monitored in RabbitMQ
                cfg.ReceiveEndpoint("saga-undo-state-1", e =>
                {
                    // STEP A: Circuit Breaker (The Shield)
                    // Stops the consumer if the failure rate is too high to protect the DB
                    e.UseCircuitBreaker(cb =>
                    {
                        cb.TrackingPeriod = TimeSpan.FromMinutes(1);
                        cb.TripThreshold = 15;      // Trip if 15% of messages fail
                        cb.ActiveThreshold = 2;    // Minimum 2 messages before tripping
                        cb.ResetInterval = TimeSpan.FromMinutes(5);
                    });

                    // STEP B: Exponential Retry (The Band-Aid)
                    // Handles transient network/db blips before reaching the breaker
                    e.UseMessageRetry(r => r.Exponential(
                        5,                          // Try 5 times
                        TimeSpan.FromSeconds(2),    // Start with 2s delay
                        TimeSpan.FromSeconds(30),   // Max 30s delay
                        TimeSpan.FromSeconds(5)));  // 5s interval increment

                    e.ConfigureConsumer<UndoState1Consumer>(context);
                });

                cfg.ReceiveEndpoint("saga-undo-state-2", e =>
                {
                    e.UseCircuitBreaker(cb =>
                    {
                        cb.TrackingPeriod = TimeSpan.FromMinutes(1);
                        cb.TripThreshold = 15;
                        cb.ActiveThreshold = 1;
                        cb.ResetInterval = TimeSpan.FromMinutes(5);
                    });

                    e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5))); // Simpler fixed interval

                    e.ConfigureConsumer<UndoState2Consumer>(context);
                });

                // 2. Global Configuration for Execute Steps & Saga
                // We set a lighter retry policy for normal business logic
                cfg.UseMessageRetry(r => r.Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)));

                // 3. Auto-Configure everything else
                // This handles ExecuteState1, ExecuteState2, ExecuteState3, and the Saga itself.
                // MassTransit is smart enough not to double-configure the Undo consumers we did above.
                cfg.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("SagaDemo", false));
            });
        });
        return services;
    }
}
