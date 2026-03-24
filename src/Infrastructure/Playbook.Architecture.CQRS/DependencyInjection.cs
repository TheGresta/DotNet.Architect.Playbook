using FluentValidation;

using Playbook.Architecture.CQRS.Application.Common.Behaviors;
using Playbook.Architecture.CQRS.Application.Common.Interfaces;
using Playbook.Architecture.CQRS.Infrastructure.Persistence;
using Playbook.Architecture.CQRS.Middlewares;

namespace Playbook.Architecture.CQRS;

/// <summary>
/// Provides centralized extension methods for the <see cref="IServiceCollection"/> to configure
/// the Application and Infrastructure layers of the system. This class establishes the MediatR 
/// pipeline order, registers validators, and sets up dependency injection for repositories and services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Configures the Application layer by registering MediatR, its pipeline behaviors, and FluentValidation validators.
    /// The order of the "AddOpenBehavior" calls is critical as it defines the middleware execution stack.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The modified <see cref="IServiceCollection"/> for method chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Capture the current assembly once to avoid multiple reflection calls during registration.
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);

            // 1. Global Exception Handling (The outermost layer)
            // Acts as the "Safety Net." It ensures that any unhandled exception in the pipeline 
            // is caught, logged, and returned as a structured ErrorOr result.
            config.AddOpenBehavior(typeof(ExceptionHandlingBehavior<,>));

            // 2. Logging (Observability)
            // Records the lifecycle of the request. Placed early to ensure that 
            // timing includes validation and caching overhead.
            config.AddOpenBehavior(typeof(LoggingBehavior<,>));

            // 3. Validation (The Gatekeeper)
            // Ensures that the request data is logically sound before any business logic or 
            // expensive I/O (like cache lookups) is performed.
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));

            // 4. Query Caching (The Fast Path)
            // Intercepts IQuery requests. If a hit occurs, it short-circuits the pipeline 
            // before reaching the Handler or Transaction layers.
            config.AddOpenBehavior(typeof(QueryCachingBehavior<,>));

            // 5. Transaction Management (Consistency)
            // Wraps the inner Handler in a database transaction. Ensures that 
            // Command side-effects are atomic.
            config.AddOpenBehavior(typeof(TransactionBehavior<,>));

            // 6. Cache Invalidation (The Cleanup)
            // Executes after the Handler. Only proceeds to purge keys if the 
            // operation and transaction were successful.
            config.AddOpenBehavior(typeof(CacheInvalidationBehavior<,>));
        });

        // Scans the assembly for all classes inheriting from AbstractValidator<T>.
        services.AddValidatorsFromAssembly(assembly);

        // Registers a distributed memory cache provider. 
        // This can be swapped for Redis in production without changing application code.
        services.AddDistributedMemoryCache();

        return services;
    }

    /// <summary>
    /// Configures the Infrastructure layer, registering data persistence implementations 
    /// and cross-cutting concerns like global ASP.NET Core exception handlers.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The modified <see cref="IServiceCollection"/> for method chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Registration of the repository as a Singleton to maintain the in-memory state
        // across different HTTP requests in this demonstration.
        services.AddSingleton<IProductRepository, InMemoryProductRepository>();

        // Registers the standard .NET 8 Exception Handler and Problem Details support
        // for consistent RFC 7807 error responses at the API level.
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}
