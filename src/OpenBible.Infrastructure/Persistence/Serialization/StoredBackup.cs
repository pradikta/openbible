namespace OpenBible.Infrastructure.Persistence.Serialization;

internal sealed record StoredBackup(
    string Format,
    int Version,
    DateTimeOffset ExportedAt,
    StoredUserData? UserData,
    StoredStudyData? Study,
    StoredAppSettings? Settings);
