using MassTransit;

using Playbook.Messaging.MassTransit.Domain;
using Playbook.Messaging.MassTransit.Infrastructure.Persistence;

namespace Playbook.Messaging.MassTransit.Infrastructure.Messaging;

public static class MessagingRegistration
{
    public static IServiceCollection AddEnterpriseMessaging(this IServiceCollection services, IConfiguration config)
    {
        // Use Bind/Validate for cleaner options handling
        var busOptions = config.GetSection(MessageBusOptions.SectionName).Get<MessageBusOptions>()
                         ?? throw new InvalidOperationException("Messaging configuration section is missing.");

        services.AddMassTransit(x =>
        {
            // Use KebabCase for all consumers/sagas by default
            x.SetKebabCaseEndpointNameFormatter();

            x.AddConsumers(typeof(MessagingRegistration).Assembly);

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

                // Global retry for the Saga and other consumers
                cfg.UseMessageRetry(r => r.Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)));

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
