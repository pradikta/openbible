using OpenBible.Domain.Entities;

namespace OpenBible.Domain.Interfaces;

public interface IYesFileReader
{
    Task<BibleVersion> ReadMetadataAsync(Stream stream, CancellationToken ct = default);
    IAsyncEnumerable<Verse> ReadVersesAsync(Stream stream, CancellationToken ct = default);
    IAsyncEnumerable<Pericope> ReadPericopesAsync(Stream stream, CancellationToken ct = default);
}
