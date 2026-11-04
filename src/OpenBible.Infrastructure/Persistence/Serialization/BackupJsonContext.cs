using System.Text.Json.Serialization;

namespace OpenBible.Infrastructure.Persistence.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(StoredBackup))]
internal sealed partial class BackupJsonContext : JsonSerializerContext;
