using System.Runtime.CompilerServices;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Infrastructure.Sword;

/// <summary>
/// Reads CrossWire SWORD Bibles packaged as a ZIP containing <c>mods.d/*.conf</c> and
/// <c>modules/texts/ztext/...</c>. Supports zText/zText4 with ZIP compression and KJV versification.
/// </summary>
public sealed class SwordFormatReader : IBibleFormatReader
{
    public BibleFormat Format => BibleFormat.Sword;

    public Task<BibleVersion> ReadMetadataAsync(Stream stream, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        using var archive = SwordArchive.Open(stream);
        var conf = SwordModuleConfParser.Parse(archive.ConfText, archive.ConfName);
        var reader = new SwordZTextReader(archive, conf);
        return Task.FromResult(reader.ReadMetadata());
    }

    public async IAsyncEnumerable<Verse> ReadVersesAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var archive = SwordArchive.Open(stream);
        var conf = SwordModuleConfParser.Parse(archive.ConfText, archive.ConfName);
        var reader = new SwordZTextReader(archive, conf);

        foreach (var verse in reader.ReadVerses(ct))
        {
            ct.ThrowIfCancellationRequested();
            yield return verse;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async IAsyncEnumerable<Pericope> ReadPericopesAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        yield break;
    }
}
