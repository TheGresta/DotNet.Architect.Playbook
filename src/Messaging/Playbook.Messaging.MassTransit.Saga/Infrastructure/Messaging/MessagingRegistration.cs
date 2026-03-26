using MassTransit;

using Playbook.Messaging.MassTransit.Saga.Domain;
using Playbook.Messaging.MassTransit.Saga.Infrastructure.Persistence;

namespace Playbook.Messaging.MassTransit.Saga.Infrastructure.Messaging;

/// <summary>
/// Provides extension methods for the <see cref="IServiceCollection"/> to centralize and simplify 
/// the registration of MassTransit-based messaging infrastructure, including Sagas and RabbitMQ transport.
/// </summary>
public static class MessagingRegistration
{
    /// <summary>
    /// Configures and adds enterprise-grade messaging services to the DI container.
    /// This includes automatic consumer discovery, Saga state machine registration with Entity Framework persistence,
    /// and RabbitMQ transport configuration with global retry policies.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="config">The <see cref="IConfiguration"/> instance to retrieve <see cref="MessageBusOptions"/> from.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the required messaging configuration section is missing.</exception>
    public static IServiceCollection AddEnterpriseMessaging(this IServiceCollection services, IConfiguration config)
    {
        // Use Bind/Validate for cleaner options handling
        // Ensures the application fails fast if critical infrastructure configuration is absent.
        var busOptions = config.GetSection(MessageBusOptions.SectionName).Get<MessageBusOptions>()
                         ?? throw new InvalidOperationException("Messaging configuration section is missing.");

        services.AddMassTransit(x =>
        {
            // Use KebabCase for all consumers/sagas by default
            // Standardizes queue naming conventions across the broker (e.g., 'workflow-state').
            x.SetKebabCaseEndpointNameFormatter();

            // Automatically scan the current assembly for any classes implementing IConsumer.
            x.AddConsumers(typeof(MessagingRegistration).Assembly);

            // Register the state machine and define its persistence layer using Entity Framework Core.
            x.AddSagaStateMachine<WorkflowStateMachine, WorkflowState>()
                .EntityFrameworkRepository(r =>
                {
                    r.ExistingDbContext<AppDbContext>();
                    r.UsePostgres();
                });

            x.UsingRabbitMq((context, cfg) =>
            {
                // Configure the connection parameters for the RabbitMQ host.
                cfg.Host(busOptions.Host, "/", h =>
                {
                    h.Username(busOptions.Username);
                    h.Password(busOptions.Password);
                });

                // Global retry for the Saga and other consumers
                // Implements an incremental backoff strategy to handle transient failures or database deadlocks.
                cfg.UseMessageRetry(r => r.Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)));

                // Automatically configures endpoints for all registered consumers and sagas.
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
