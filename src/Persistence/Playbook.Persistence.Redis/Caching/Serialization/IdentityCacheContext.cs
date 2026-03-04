using System.Text.Json.Serialization;
using Playbook.Persistence.Redis.Application.Models;

namespace Playbook.Persistence.Redis.Caching.Serialization;

// --- Identity Module Context ---
[JsonSerializable(typeof(UserDto))]
[JsonSerializable(typeof(List<UserDto>))]
[JsonSerializable(typeof(CacheEnvelope<UserDto>))] // Required for the wrapper!
internal partial class IdentityCacheContext : JsonSerializerContext { }
