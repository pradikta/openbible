using OpenBible.Domain.Enums;
using OpenBible.Domain.ValueObjects;
using Xunit;

namespace OpenBible.Domain.Tests;

public sealed class VerseRangeTests
{
    [Fact]
    public void Range_WithinSameChapter_ShouldContainMiddleVerse()
    {
        var start = new VerseReference(BookId.John, 3, 16);
        var end = new VerseReference(BookId.John, 3, 18);
        var range = new VerseRange(start, end);

        var mid = new VerseReference(BookId.John, 3, 17);
        var before = new VerseReference(BookId.John, 3, 15);
        var after = new VerseReference(BookId.John, 3, 19);

        Assert.True(range.Contains(mid));
        Assert.True(range.Contains(start));
        Assert.True(range.Contains(end));
        Assert.False(range.Contains(before));
        Assert.False(range.Contains(after));
    }

    [Fact]
    public void Range_DifferentBooks_ShouldThrowArgumentException()
    {
        var start = new VerseReference(BookId.Genesis, 1, 1);
        var end = new VerseReference(BookId.Exodus, 1, 1);

        Assert.Throws<ArgumentException>(() => new VerseRange(start, end));
    }

    [Fact]
    public void Range_EndBeforeStart_ShouldThrowArgumentException()
    {
        var start = new VerseReference(BookId.Genesis, 1, 10);
        var end = new VerseReference(BookId.Genesis, 1, 5);

        Assert.Throws<ArgumentException>(() => new VerseRange(start, end));
    }
}
