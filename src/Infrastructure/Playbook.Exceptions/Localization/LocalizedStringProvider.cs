using Microsoft.Extensions.Localization;
using Playbook.Exceptions.Constants;

namespace Playbook.Exceptions.Localization;

public sealed class LocalizedStringProvider(
    IStringLocalizer<SharedResources> localizer,
    ILogger<LocalizedStringProvider> logger) : ILocalizedStringProvider
{
    public string Get(string key, params object[] args)
    {
        var localizedString = localizer[key, args];

        Console.WriteLine($"DEBUG: Key: {key}, SearchedLocation: {localizedString.SearchedLocation}");

        if (localizedString.ResourceNotFound)
        {
            logger.LogWarning("Localization key not found: {Key}. Falling back to default.", key);

            // Fallback to the generic error message we defined in Step 3
            return localizer[LocalizationKeys.InternalServerTitle];
        }

        return localizedString.Value;
    }
}