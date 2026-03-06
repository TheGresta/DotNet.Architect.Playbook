using Microsoft.Extensions.Localization;
using Playbook.Exceptions.Abstraction;
using Playbook.Exceptions.Constants;
using Playbook.Exceptions.Resources;

namespace Playbook.Exceptions.Core;

public sealed class LocalizedStringProvider(
    IStringLocalizerFactory factory,
    ILogger<LocalizedStringProvider> logger) : ILocalizedStringProvider
{
    public string Get(string key, params object[] args)
    {
        try
        {
            // 1. Determine which Resource File to use based on the Prefix
            var resourceType = GetResourceType(key);
            var localizer = factory.Create(resourceType);

            // 2. Strip the prefix for the actual lookup
            // Example: "VAL_REQUIRED" -> "REQUIRED"
            string cleanKey = key.Contains('_') ? key.Split('_', 2)[1] : key;

            var result = localizer[cleanKey, args];

            if (result.ResourceNotFound)
            {
                logger.LogWarning("Key {CleanKey} not found in {ResourceName}. Full Key: {Key}",
                    cleanKey, resourceType.Name, key);

                // Return the raw key so developers can identify missing translations
                return key;
            }

            return result.Value;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Localization failed for key: {Key}", key);
            return key; // Fail safe
        }
    }

    private static Type GetResourceType(string key)
    {
        if (string.IsNullOrEmpty(key)) return typeof(SharedResources);

        return key switch
        {
            var k when k.StartsWith(LocalizationPrefixes.Info) => typeof(InfoResources),
            var k when k.StartsWith(LocalizationPrefixes.Detail) => typeof(DetailResources),
            var k when k.StartsWith(LocalizationPrefixes.Resource) => typeof(ResourceResources),
            var k when k.StartsWith(LocalizationPrefixes.Validation) => typeof(ValidationResources),
            var k when k.StartsWith(LocalizationPrefixes.Rule) => typeof(BusinessRuleResources),
            _ => typeof(SharedResources)
        };
    }
}