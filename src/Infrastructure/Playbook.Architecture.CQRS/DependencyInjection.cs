using FluentValidation;

using Playbook.Architecture.CQRS.Application.Common.Behaviors;
using Playbook.Architecture.CQRS.Application.Common.Interfaces;
using Playbook.Architecture.CQRS.Infrastructure.Errors;
using Playbook.Architecture.CQRS.Infrastructure.Persistence;

namespace Playbook.Architecture.CQRS;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);

            // 1. Global Exception Handling (The outermost layer)
            // Catches anything that explodes in any other behavior or handler.
            config.AddOpenBehavior(typeof(ExceptionHandlingBehavior<,>));

            // 2. Logging (Observability)
            // Records the start/end and timing of the request.
            config.AddOpenBehavior(typeof(LoggingBehavior<,>));

            // 3. Validation (The Gatekeeper)
            // CRITICAL: Must be before Caching. We don't want to hit the cache
            // or check Redis for a request that has an invalid ID format.
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));

            // 4. Query Caching (The Fast Path)
            // Returns early for GETs. If it's a Command, it just passes through.
            config.AddOpenBehavior(typeof(QueryCachingBehavior<,>));

            // 5. Transaction Management (Consistency)
            // Opens the SQL Transaction.
            config.AddOpenBehavior(typeof(TransactionBehavior<,>));

            // 6. Cache Invalidation (The Cleanup)
            // Runs after the Handler. Only clears cache if Transaction committed.
            config.AddOpenBehavior(typeof(CacheInvalidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        // Big Tech Upgrade: Switch from MemoryCache to Distributed
        services.AddDistributedMemoryCache();

        return services;
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Simple Singleton for our In-Memory state
        services.AddSingleton<IProductRepository, InMemoryProductRepository>();

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}
