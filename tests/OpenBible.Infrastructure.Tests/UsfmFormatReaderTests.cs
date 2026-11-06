using System.IO.Compression;
using System.Text;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.ValueObjects;
using OpenBible.Infrastructure.Usfm;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class UsfmFormatReaderTests
{
    private const string GenesisUsfm = """
        \id GEN
        \h Genesis
        \c 1
        \s The Creation
        \v 1 In the beginning \add God\add* created the heavens.
        \v 2 And the earth was \w without form|lemma="x"\w*.
        """;

    [Fact]
    public async Task ReadMetadata_MapsBooksChaptersAndVerses()
    {
        await using var stream = Zip(("GEN.usfm", GenesisUsfm));

        var version = await new UsfmFormatReader().ReadMetadataAsync(stream);

        Assert.Equal(BibleFormat.Usfm, version.Format);
        var book = Assert.Single(version.Books);
        Assert.Equal(BookId.Genesis, book.BookId);
        Assert.Equal("Genesis", book.FullName);
        Assert.Equal(1, book.ChapterCount);
        Assert.Equal(new[] { 2 }, book.VerseCounts);
    }

    [Fact]
    public async Task ReadVerses_FlattensMarkers()
    {
        await using var stream = Zip(("GEN.usfm", GenesisUsfm));
        var verses = new List<Verse>();
        await foreach (var verse in new UsfmFormatReader().ReadVersesAsync(stream))
        {
            verses.Add(verse);
        }

        Assert.Equal(2, verses.Count);
        Assert.Equal("In the beginning God created the heavens.", verses[0].Text);
        Assert.Equal("And the earth was without form.", verses[1].Text);
    }

    [Fact]
    public async Task ReadPericopes_MapsHeadings()
    {
        await using var stream = Zip(("GEN.usfm", GenesisUsfm));
        var pericopes = new List<Pericope>();
        await foreach (var pericope in new UsfmFormatReader().ReadPericopesAsync(stream))
        {
            pericopes.Add(pericope);
        }

        var single = Assert.Single(pericopes);
        Assert.Equal("The Creation", single.Title);
        Assert.Equal(new VerseReference(BookId.Genesis, 1, 1), single.Reference);
    }

    [Fact]
    public async Task ReadMetadata_PlainFile_Works()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(GenesisUsfm));

        var version = await new UsfmFormatReader().ReadMetadataAsync(stream);

        Assert.Single(version.Books);
    }

    [Fact]
    public async Task ReadMetadata_UnknownBookCode_Throws()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("\\id XYZ\n\\c 1\n\\v 1 text"));

        await Assert.ThrowsAsync<InvalidDataException>(() => new UsfmFormatReader().ReadMetadataAsync(stream));
    }

    private static MemoryStream Zip(params (string Name, string Content)[] files)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in files)
            {
                var entry = archive.CreateEntry(name);
                using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
                writer.Write(content);
            }
        }

        stream.Position = 0;
        return stream;
    }
}
