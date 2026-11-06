using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;

namespace OpenBible.Domain.Interfaces;

public interface IBibleFormatReader
{
    BibleFormat Format { get; }
    Task<BibleVersion> ReadMetadataAsync(Stream stream, CancellationToken ct = default);
    IAsyncEnumerable<Verse> ReadVersesAsync(Stream stream, CancellationToken ct = default);
    IAsyncEnumerable<Pericope> ReadPericopesAsync(Stream stream, CancellationToken ct = default);
}
