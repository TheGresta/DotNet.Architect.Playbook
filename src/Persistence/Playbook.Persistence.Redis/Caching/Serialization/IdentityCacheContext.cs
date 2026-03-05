using System.Text.Json.Serialization;
using Playbook.Persistence.Redis.Models;

namespace Playbook.Persistence.Redis.Caching.Serialization;

// --- Identity Module Context ---
[JsonSerializable(typeof(UserDto))]
[JsonSerializable(typeof(List<UserDto>))]
[JsonSerializable(typeof(CacheEnvelope<UserDto>))]
[JsonSerializable(typeof(CacheEnvelope<List<UserDto>>))]
internal partial class IdentityCacheContext : JsonSerializerContext;
