using System.Text.Json.Serialization;

using Playbook.Messaging.RabbitMQ.Models;

namespace Playbook.Messaging.RabbitMQ.Messaging.Internal.Serialization;

// We use a marker interface or object to allow any class to be serialized 
// without needing to update this context for every single new message type.
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(OrderCreated))]
[JsonSourceGenerationOptions(
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class MessagingJsonContext : JsonSerializerContext;
