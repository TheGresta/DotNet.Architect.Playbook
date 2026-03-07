using Microsoft.AspNetCore.Mvc.Infrastructure;

using Playbook.Exceptions.Abstraction;
using Playbook.Exceptions.Core;
using Playbook.Exceptions.Infrastructure;

namespace Playbook.Exceptions;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> and <see cref="IApplicationBuilder"/> 
/// to encapsulate and simplify the registration of the global error handling and localization infrastructure.
/// </summary>
public static class InfrastructureExtensions
{
    /// <summary>
    /// Registers all necessary services for the domain-driven error handling framework, 
    /// including localization, exception mapping, and the .NET 8 global exception handler.
    /// </summary>
    /// <param name="services">The service collection to augment.</param>
    /// <returns>The modified <see cref="IServiceCollection"/> for method chaining.</returns>
    public static IServiceCollection AddInfrastructureErrorHandling(this IServiceCollection services)
    {
        // 1. Native .NET 8 Problem Details
        // Configures built-in support for generating RFC 7807 compliant responses.
        services.AddProblemDetails();

        // 2. Localization Configuration
        // Adds the core localization services required by IStringLocalizer.
        services.AddLocalization();

        // 3. Register the Global Exception Handler (Order matters in .NET 8)
        // IExceptionHandler implementations are processed in the order they are registered.
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // 4. Component Registration
        // Singleton is appropriate as these are stateless logic providers that do not hold request context.
        services.AddSingleton<IExceptionMapper, DomainExceptionMapper>();
        services.AddSingleton<ILocalizedStringProvider, LocalizedStringProvider>();

        // Override the default Factory
        // Replaces the internal Microsoft implementation with our localized version to intercept 
        // framework-level errors (like 400 Bad Request from [ApiController]).
        services.AddSingleton<ProblemDetailsFactory, GlobalProblemDetailsFactory>();

        return services;
    }

    /// <summary>
    /// Configures the request pipeline to utilize the infrastructure error handling middleware.
    /// Ensures that localization is established before the exception handler runs to provide translated messages.
    /// </summary>
    /// <param name="app">The application builder instance.</param>
    /// <returns>The modified <see cref="IApplicationBuilder"/>.</returns>
    public static IApplicationBuilder UseInfrastructureErrorHandling(this IApplicationBuilder app)
    {
        // 1. Determine culture BEFORE handling exceptions so the handler sees the correct culture
        // This is a critical architectural requirement: the culture must be set early in the pipeline.
        var supportedCultures = new[] { "en-US", "tr-TR" };
        var localizationOptions = new RequestLocalizationOptions()
            .SetDefaultCulture(supportedCultures[0])
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures);

        // Move the culture provider to the top of the stack.
        app.UseRequestLocalization(localizationOptions);

        // 2. Standard .NET Exception Middleware (utilizes IExceptionHandler)
        // This middleware catches unhandled exceptions and delegates them to the GlobalExceptionHandler.
        app.UseExceptionHandler();

        // 3. Optional: Map ProblemDetails for standard status code results (e.g., return NotFound())
        // Converts empty non-success responses (e.g., 401, 404) into the standardized ApiErrorResponse format.
        app.UseStatusCodePages();

        return app;
    }
}
