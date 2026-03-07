using Microsoft.AspNetCore.Mvc.Infrastructure;

using Playbook.Exceptions.Abstraction;
using Playbook.Exceptions.Core;
using Playbook.Exceptions.Infrastructure;

namespace Playbook.Exceptions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructureErrorHandling(this IServiceCollection services)
    {
        // 1. Native .NET 8 Problem Details
        services.AddProblemDetails();

        // 2. Localization Configuration
        services.AddLocalization();

        // 3. Register the Global Exception Handler (Order matters in .NET 8)
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // 4. Component Registration
        // Singleton is appropriate as these are stateless logic providers
        services.AddSingleton<IExceptionMapper, DomainExceptionMapper>();
        services.AddSingleton<ILocalizedStringProvider, LocalizedStringProvider>();

        // Override the default Factory
        services.AddSingleton<ProblemDetailsFactory, GlobalProblemDetailsFactory>();

        return services;
    }

    public static IApplicationBuilder UseInfrastructureErrorHandling(this IApplicationBuilder app)
    {
        // 1. Determine culture BEFORE handling exceptions so the handler sees the correct culture
        var supportedCultures = new[] { "en-US", "tr-TR" };
        var localizationOptions = new RequestLocalizationOptions()
            .SetDefaultCulture(supportedCultures[0])
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures);

        // Move the culture provider to the top of the stack
        app.UseRequestLocalization(localizationOptions);

        // 2. Standard .NET Exception Middleware (utilizes IExceptionHandler)
        app.UseExceptionHandler();

        // 3. Optional: Map ProblemDetails for standard status code results (e.g., return NotFound())
        app.UseStatusCodePages();

        return app;
    }
}
