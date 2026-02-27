using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace Playbook.Persistence.MongoDB.Infrastructure.Contexts;

/// <summary>
/// Ensures global MongoDB driver configurations (Conventions, Serializers) 
/// are registered exactly once during the application lifecycle.
/// </summary>
internal static class MongoInitializer
{
    private static volatile bool _initialized = false;
    private static readonly object _lock = new();

    /// <summary>
    /// Executes the one-time registration of MongoDB conventions and serializers.
    /// Thread-safe to prevent "Serializer already registered" exceptions.
    /// </summary>
    public static void Initialize()
    {
        // Summary: Fast-check before locking to optimize performance after initialization.
        if (_initialized)
        {
            return;
        }

        lock (_lock)
        {
            // Summary: Double-check lock to ensure only one thread performs the registration.
            if (_initialized)
            {
                return;
            }

            // Summary: Configures how C# classes map to BSON (e.g., camelCase, String-based Enums).
            var pack = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true),
                new IgnoreIfDefaultConvention(false),
                new EnumRepresentationConvention(BsonType.String)
            };
            ConventionRegistry.Register("GlobalConventions", pack, _ => true);

            // Summary: Standardizes GUID storage to the modern UUID format (Binary subtype 0x04).
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

            // Summary: Ensures high-precision Decimals are stored as Decimal128 for financial accuracy.
            BsonSerializer.RegisterSerializer(new DecimalSerializer(BsonType.Decimal128));

            _initialized = true;
        }
    }
}
