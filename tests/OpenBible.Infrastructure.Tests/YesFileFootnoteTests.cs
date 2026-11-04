using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.Exceptions;
using OpenBible.Domain.ValueObjects;
using OpenBible.Infrastructure.Persistence;
using OpenBible.Infrastructure.YesFile;
using Xunit;

namespace OpenBible.Infrastructure.Tests;

public sealed class YesFileFootnoteTests
{
    private readonly YesFileParser _parser = new();

    [Fact]
    public async Task ReadVersesAsync_ARifSections_AttachNotesToTheirVerses()
    {
        using var stream = Yes2TestFile.Create(
            ("versionInfo", Yes2TestFile.MetadataMap()),
            ("booksInfo", Yes2TestFile.GenesisChapterOneBooksInfo(2)),
            ("text", Yes2TestFile.VerseTextSection("@@First @<f1@>@/@<x1@>@/", "Second")),
            ("footnotes", Yes2TestFile.ArifSection(
                (Yes2TestFile.Arif(BookId.Genesis, 1, 1, 1), "First note"),
                (Yes2TestFile.Arif(BookId.Genesis, 1, 1, 2), "Second note"),
                (Yes2TestFile.Arif(BookId.Genesis, 1, 2, 1), "Third note"))),
            ("xrefs", Yes2TestFile.ArifSection(
                (Yes2TestFile.Arif(BookId.Genesis, 1, 1, 1), "John 1:1"))));

        var verses = await ReadAllAsync(stream);

        Assert.Equal(2, verses.Count);
        Assert.Equal("@@First @<f1@>@/@<x1@>@/", verses[0].Text);

        Assert.Equal(2, verses[0].Footnotes.Count);
        Assert.Equal(1, verses[0].Footnotes[0].Index);
        Assert.Equal("First note", verses[0].Footnotes[0].Content);
        Assert.Equal(2, verses[0].Footnotes[1].Index);
        Assert.Equal("Second note", verses[0].Footnotes[1].Content);
        Assert.Equal(new VerseReference(BookId.Genesis, 1, 1), verses[0].Footnotes[0].Reference);

        var xref = Assert.Single(verses[0].XRefs);
        Assert.Equal(1, xref.Index);
        Assert.Equal("John 1:1", xref.Content);

        var secondFootnote = Assert.Single(verses[1].Footnotes);
        Assert.Equal("Third note", secondFootnote.Content);
        Assert.Equal(2, secondFootnote.Reference.Verse);
        Assert.Empty(verses[1].XRefs);
    }

    [Fact]
    public async Task ReadVersesAsync_SnappyCompressedFootnotes_ReadsEntries()
    {
        var payload = Yes2TestFile.ArifSection((Yes2TestFile.Arif(BookId.Genesis, 1, 1, 1), "Snappy note"));
        var block = Yes2TestFile.SnappyLiteral(payload);
        var attributes = Yes2TestFile.SnappyBlocksAttributes(payload.Length, block.Length);
        using var stream = Yes2TestFile.CreateWithAttributes(
            ("versionInfo", null, Yes2TestFile.MetadataMap()),
            ("booksInfo", null, Yes2TestFile.GenesisChapterOneBooksInfo()),
            ("text", null, Yes2TestFile.VerseTextSection("Verse")),
            ("footnotes", attributes, block));

        var verses = await ReadAllAsync(stream);

        var footnote = Assert.Single(verses[0].Footnotes);
        Assert.Equal("Snappy note", footnote.Content);
    }

    [Fact]
    public async Task ReadVersesAsync_MissingNoteSections_LeavesNotesEmpty()
    {
        using var stream = Yes2TestFile.Create(
            ("versionInfo", Yes2TestFile.MetadataMap()),
            ("booksInfo", Yes2TestFile.GenesisChapterOneBooksInfo()),
            ("text", Yes2TestFile.VerseTextSection("Verse")));

        var verses = await ReadAllAsync(stream);

        Assert.Empty(verses[0].Footnotes);
        Assert.Empty(verses[0].XRefs);
    }

    [Fact]
    public async Task ReadVersesAsync_ZeroEntryIndex_ThrowsYesFileCorruptException()
    {
        using var stream = Yes2TestFile.Create(
            ("versionInfo", Yes2TestFile.MetadataMap()),
            ("booksInfo", Yes2TestFile.GenesisChapterOneBooksInfo()),
            ("text", Yes2TestFile.VerseTextSection("Verse")),
            ("footnotes", Yes2TestFile.ArifSection((Yes2TestFile.Arif(BookId.Genesis, 1, 1, 1) - 1, "Bad note"))));

        await Assert.ThrowsAsync<YesFileCorruptException>(async () => await ReadAllAsync(stream));
    }

    [Fact]
    public async Task ReadVersesAsync_EntryOffsetOutsideSection_ThrowsYesFileCorruptException()
    {
        using var section = new MemoryStream();
        section.WriteByte(1);
        Yes2TestFile.WriteInt32(section, 1);
        Yes2TestFile.WriteInt32(section, Yes2TestFile.Arif(BookId.Genesis, 1, 1, 1));
        Yes2TestFile.WriteInt32(section, 99);
        using var stream = Yes2TestFile.Create(
            ("versionInfo", Yes2TestFile.MetadataMap()),
            ("booksInfo", Yes2TestFile.GenesisChapterOneBooksInfo()),
            ("text", Yes2TestFile.VerseTextSection("Verse")),
            ("footnotes", section.ToArray()));

        await Assert.ThrowsAsync<YesFileCorruptException>(async () => await ReadAllAsync(stream));
    }

    [Fact]
    public async Task ReadVersesAsync_UnsupportedArifDataFormatVersion_ThrowsNotSupportedException()
    {
        using var stream = Yes2TestFile.Create(
            ("versionInfo", Yes2TestFile.MetadataMap()),
            ("booksInfo", Yes2TestFile.GenesisChapterOneBooksInfo()),
            ("text", Yes2TestFile.VerseTextSection("Verse")),
            ("xrefs", [0x02, 0x00, 0x00, 0x00, 0x00]));

        await Assert.ThrowsAsync<NotSupportedException>(async () => await ReadAllAsync(stream));
    }

    [Fact]
    public async Task SaveVersionAsync_KeepsNotesAvailableForChapterLookup()
    {
        using var stream = Yes2TestFile.Create(
            ("versionInfo", Yes2TestFile.MetadataMap()),
            ("booksInfo", Yes2TestFile.GenesisChapterOneBooksInfo()),
            ("text", Yes2TestFile.VerseTextSection("Verse")),
            ("footnotes", Yes2TestFile.ArifSection((Yes2TestFile.Arif(BookId.Genesis, 1, 1, 2), "Saved note"))),
            ("xrefs", Yes2TestFile.ArifSection((Yes2TestFile.Arif(BookId.Genesis, 1, 1, 1), "Rom 5:8"))));
        var metadata = await _parser.ReadMetadataAsync(stream);
        var repository = new InMemoryBibleRepository();

        await repository.SaveVersionAsync(metadata, _parser.ReadVersesAsync(stream));

        var verses = await repository.GetChapterVersesAsync(metadata.Id, BookId.Genesis, 1);
        var footnote = Assert.Single(verses[0].Footnotes);
        Assert.Equal("Saved note", footnote.Content);
        var xref = Assert.Single(verses[0].XRefs);
        Assert.Equal("Rom 5:8", xref.Content);
    }

    private async Task<List<Verse>> ReadAllAsync(Stream stream)
    {
        var verses = new List<Verse>();
        await foreach (var verse in _parser.ReadVersesAsync(stream))
        {
            verses.Add(verse);
        }

        return verses;
    }
}
