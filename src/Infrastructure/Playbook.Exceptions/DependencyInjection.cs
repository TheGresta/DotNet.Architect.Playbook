using Playbook.Exceptions.Mapping;

namespace Playbook.Exceptions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureErrorHandling(this IServiceCollection services)
    {
        // 1. Native .NET 8 Problem Details
        services.AddProblemDetails();

        // 2. Localization Configuration
        services.AddLocalization(options => options.ResourcesPath = "Resources");

        // 3. Register the Global Exception Handler
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddSingleton<IExceptionMapper, DomainExceptionMapper>();

        return services;
    }

    public static IApplicationBuilder UseInfrastructureErrorHandling(this IApplicationBuilder app)
    {
        // 1. Determine culture from Request headers (Accept-Language)
        var supportedCultures = new[] { "en-US", "tr-TR" };
        var localizationOptions = new RequestLocalizationOptions()
            .SetDefaultCulture(supportedCultures[0])
            .AddSupportedCultures(supportedCultures)
            .AddSupportedUICultures(supportedCultures);

        app.UseRequestLocalization(localizationOptions);

        // 2. Enable the Exception Handler Middleware
        app.UseExceptionHandler();

        return app;
    }
}
