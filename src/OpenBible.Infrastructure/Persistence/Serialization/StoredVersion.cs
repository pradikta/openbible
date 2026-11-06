using OpenBible.Domain.Enums;

namespace OpenBible.Infrastructure.Persistence.Serialization;

internal sealed record StoredVersion(
    string Id,
    string Title,
    string Language,
    string Description,
    StoredBook[] Books,
    StoredVerse[] Verses,
    StoredPericope[] Pericopes,
    BibleFormat Format = BibleFormat.Yes,
    string? SourceId = null,
    string? License = null);
