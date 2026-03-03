using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Playbook.Persistence.EntityFramework.Persistence.Context;

namespace Playbook.Persistence.EntityFramework.Persistence.Extensions;

/// <summary>
/// Provides extension methods for the EF Core <see cref="PropertyBuilder{TProperty}"/> 
/// to facilitate metadata-driven data behaviors.
/// </summary>
internal static class PropertyBuilderExtensions
{
    /// <summary>
    /// The unique key used to identify properties that require encryption within the EF Core metadata model.
    /// </summary>
    public const string EncryptionAnnotation = "AesEncryptionFlag";

    /// <summary>
    /// Marks a string property for transparent AES encryption.
    /// </summary>
    /// <param name="propertyBuilder">The <see cref="PropertyBuilder{TProperty}"/> for the property being configured.</param>
    /// <param name="encrypt">
    /// <see langword="true"/> to enable encryption; <see langword="false"/> to explicitly disable it. 
    /// The default is <see langword="true"/>.
    /// </param>
    /// <returns>The same <see cref="PropertyBuilder{TProperty}"/> instance so that multiple calls can be chained.</returns>
    /// <remarks>
    /// <para>
    /// <b>Logic:</b>
    /// Instead of applying a Value Converter directly, this method attaches a metadata "Annotation" to the property. 
    /// During the model creation phase in <see cref="ApplicationDbContext"/>, the system scans for this 
    /// <see cref="EncryptionAnnotation"/> and applies the cryptographic converter dynamically.
    /// </para>
    /// <code language="csharp">
    /// // Usage in Entity Configuration:
    /// builder.Property(x => x.Email).Encrypt();
    /// </code>
    /// </remarks>
    public static PropertyBuilder<string> Encrypt(this PropertyBuilder<string> propertyBuilder, bool encrypt = true)
    {
        return encrypt
            ? propertyBuilder.HasAnnotation(EncryptionAnnotation, true)
            : propertyBuilder;
    }
}