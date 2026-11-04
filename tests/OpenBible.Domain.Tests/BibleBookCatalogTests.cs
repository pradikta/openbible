using OpenBible.Domain.Catalog;
using OpenBible.Domain.Enums;
using Xunit;

namespace OpenBible.Domain.Tests;

public sealed class BibleBookCatalogTests
{
    [Fact]
    public void All_ContainsProtestantCanon()
    {
        Assert.Equal(66, BibleBookCatalog.All.Count);
        Assert.Equal(BookId.Genesis, BibleBookCatalog.All[0].Id);
        Assert.Equal(BookId.Revelation, BibleBookCatalog.All[^1].Id);
        Assert.Equal(1189, BibleBookCatalog.All.Sum(book => book.ChapterCount));
    }

    [Fact]
    public void Get_KnownBooks_ReturnsChapterCounts()
    {
        Assert.Equal(50, BibleBookCatalog.Get(BookId.Genesis).ChapterCount);
        Assert.Equal(150, BibleBookCatalog.Get(BookId.Psalms).ChapterCount);
        Assert.Equal(1, BibleBookCatalog.Get(BookId.Obadiah).ChapterCount);
        Assert.Equal(22, BibleBookCatalog.Get(BookId.Revelation).ChapterCount);
        Assert.Equal("1 Samuel", BibleBookCatalog.Get(BookId.FirstSamuel).FullName);
    }

    [Fact]
    public void TryNext_CrossesBookBoundary()
    {
        var moved = BibleBookCatalog.TryNext(BookId.Genesis, 50, out var book, out var chapter);

        Assert.True(moved);
        Assert.Equal(BookId.Exodus, book);
        Assert.Equal(1, chapter);
    }

    [Fact]
    public void TryPrevious_FromMatthew_LandsOnMalachi()
    {
        var moved = BibleBookCatalog.TryPrevious(BookId.Matthew, 1, out var book, out var chapter);

        Assert.True(moved);
        Assert.Equal(BookId.Malachi, book);
        Assert.Equal(4, chapter);
    }

    [Fact]
    public void TryNext_AtEndOfRevelation_ReturnsFalse()
    {
        var moved = BibleBookCatalog.TryNext(BookId.Revelation, 22, out _, out _);

        Assert.False(moved);
    }
}
