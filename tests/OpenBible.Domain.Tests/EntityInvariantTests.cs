using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.ValueObjects;
using Xunit;

namespace OpenBible.Domain.Tests;

public sealed class EntityInvariantTests
{
    private static readonly VerseReference GenesisOneOne = new(BookId.Genesis, 1, 1);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BibleVersion_WithEmptyId_ShouldThrow(string id)
    {
        Assert.Throws<ArgumentException>(() => new BibleVersion(id, "Title", "en", "Description"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BibleVersion_WithEmptyTitle_ShouldThrow(string title)
    {
        Assert.Throws<ArgumentException>(() => new BibleVersion("kjv", title, "en", "Description"));
    }

    [Fact]
    public void BibleVersion_WithNullOptionalValues_ShouldUseDefaults()
    {
        var version = new BibleVersion("kjv", "King James Version", null!, null!);

        Assert.Equal("en", version.Language);
        Assert.Equal(string.Empty, version.Description);
        Assert.Empty(version.Books);
    }

    [Fact]
    public void Book_WithNullNames_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new Book(BookId.Genesis, null!, "Genesis", 50));
        Assert.Throws<ArgumentNullException>(() => new Book(BookId.Genesis, "Gen", null!, 50));
    }

    [Fact]
    public void Book_WithNullVerseCounts_ShouldDefaultToEmpty()
    {
        var book = new Book(BookId.Genesis, "Gen", "Genesis", 50);

        Assert.Empty(book.VerseCounts);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Chapter_WithNonPositiveNumber_ShouldThrow(int chapterNumber)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Chapter(BookId.Genesis, chapterNumber, []));
    }

    [Fact]
    public void Chapter_WithNullVerses_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new Chapter(BookId.Genesis, 1, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Pericope_WithEmptyTitle_ShouldThrow(string title)
    {
        Assert.Throws<ArgumentException>(() => new Pericope(GenesisOneOne, title));
    }

    [Fact]
    public void Pericope_WithNullParallels_ShouldDefaultToEmpty()
    {
        var pericope = new Pericope(GenesisOneOne, "The Creation");

        Assert.Empty(pericope.ParallelPassages);
    }

    [Fact]
    public void Verse_WithNullText_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new Verse(GenesisOneOne, null!));
    }

    [Fact]
    public void Verse_WithNullCollections_ShouldDefaultToEmpty()
    {
        var verse = new Verse(GenesisOneOne, "In the beginning.");

        Assert.Empty(verse.Footnotes);
        Assert.Empty(verse.XRefs);
    }

    [Fact]
    public void FootnoteEntry_WithNullContent_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new FootnoteEntry(GenesisOneOne, 1, null!));
    }

    [Fact]
    public void XRefEntry_WithNullContent_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new XRefEntry(GenesisOneOne, 1, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Bookmark_WithEmptyVersionId_ShouldThrow(string versionId)
    {
        Assert.Throws<ArgumentException>(() =>
            new Bookmark(Guid.NewGuid(), versionId, GenesisOneOne, DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Highlight_WithEmptyVersionId_ShouldThrow(string versionId)
    {
        Assert.Throws<ArgumentException>(() =>
            new Highlight(Guid.NewGuid(), versionId, GenesisOneOne, HighlightColor.Yellow, DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ReadingPosition_WithEmptyVersionId_ShouldThrow(string versionId)
    {
        Assert.Throws<ArgumentException>(() =>
            new ReadingPosition(versionId, GenesisOneOne, DateTimeOffset.UtcNow));
    }
}
