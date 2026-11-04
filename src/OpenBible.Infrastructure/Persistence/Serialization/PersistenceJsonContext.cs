using System.Text.Json.Serialization;

namespace OpenBible.Infrastructure.Persistence.Serialization;

[JsonSerializable(typeof(StoredVersion))]
[JsonSerializable(typeof(StoredUserData))]
[JsonSerializable(typeof(string[]))]
internal sealed partial class PersistenceJsonContext : JsonSerializerContext;
