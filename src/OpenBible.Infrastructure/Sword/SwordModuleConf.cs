namespace OpenBible.Infrastructure.Sword;

internal sealed record SwordModuleConf(
    string Name,
    string DataPath,
    string ModDrv,
    string? Encoding,
    string BlockType,
    string CompressType,
    string Versification,
    string SourceType,
    string Title,
    string? Language,
    string? License);
