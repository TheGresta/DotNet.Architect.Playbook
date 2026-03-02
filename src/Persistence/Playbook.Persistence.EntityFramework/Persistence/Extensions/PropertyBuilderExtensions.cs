using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Playbook.Persistence.EntityFramework.Persistence.Extensions;

internal static class PropertyBuilderExtensions
{
    public const string EncryptionAnnotation = "AesEncryptionFlag";

    public static PropertyBuilder<string> Encrypt(this PropertyBuilder<string> propertyBuilder, bool encrypt = true)
    {
        return encrypt
            ? propertyBuilder.HasAnnotation(EncryptionAnnotation, true)
            : propertyBuilder;
    }
}
