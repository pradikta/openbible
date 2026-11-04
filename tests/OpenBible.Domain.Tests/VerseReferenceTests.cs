using OpenBible.Domain.Enums;
using OpenBible.Domain.ValueObjects;
using Xunit;

namespace OpenBible.Domain.Tests;

public sealed class VerseReferenceTests
{
    [Fact]
    public void Genesis_Chapter1_WholeChapter_ShouldPackTo256()
    {
        // Formula: (bookId << 16) | (chapter << 8) | verse
        // Genesis = 0, Chapter = 1, Verse = 0 => 0x000100 = 256
        var reference = new VerseReference(BookId.Genesis, 1, 0);

        var ari = reference.ToAri();

        Assert.Equal(256, ari);
        Assert.Equal(0x000100, ari);
        Assert.True(reference.IsEntireChapter);
    }

    [Fact]
    public void FromAri_256_ShouldUnpackToGenesis1_WholeChapter()
    {
        var reference = VerseReference.FromAri(256);

        Assert.Equal(BookId.Genesis, reference.Book);
        Assert.Equal(1, reference.Chapter);
        Assert.Equal(0, reference.Verse);
        Assert.True(reference.IsEntireChapter);
    }

    [Fact]
    public void Genesis_1_1_ShouldPackAndUnpackCorrectly()
    {
        // Genesis = 0, Chapter = 1, Verse = 1 => 0x000101 = 257
        var reference = new VerseReference(BookId.Genesis, 1, 1);

        var ari = reference.ToAri();
        var unpacked = VerseReference.FromAri(ari);

        Assert.Equal(0x000101, ari);
        Assert.Equal(reference, unpacked);
        Assert.False(reference.IsEntireChapter);
    }

    [Fact]
    public void Revelation_22_21_ShouldRoundtrip()
    {
        // Revelation = 65, Chapter = 22, Verse = 21
        var reference = new VerseReference(BookId.Revelation, 22, 21);

        var ari = reference.ToAri();
        var unpacked = VerseReference.FromAri(ari);

        Assert.Equal(reference, unpacked);
        Assert.Equal(BookId.Revelation, unpacked.Book);
        Assert.Equal(22, unpacked.Chapter);
        Assert.Equal(21, unpacked.Verse);
    }

    [Fact]
    public void VerseReference_Equality_ShouldBeReflexiveAndSymmetric()
    {
        var ref1 = new VerseReference(BookId.John, 3, 16);
        var ref2 = new VerseReference(BookId.John, 3, 16);
        var ref3 = new VerseReference(BookId.John, 3, 17);

        Assert.Equal(ref1, ref2);
        Assert.True(ref1 == ref2);
        Assert.False(ref1 == ref3);
        Assert.True(ref1 != ref3);
        Assert.Equal(ref1.GetHashCode(), ref2.GetHashCode());
    }

    [Fact]
    public void VerseReference_Comparison_ShouldOrderChronologically()
    {
        var gen1 = new VerseReference(BookId.Genesis, 1, 1);
        var gen2 = new VerseReference(BookId.Genesis, 1, 2);
        var exo1 = new VerseReference(BookId.Exodus, 1, 1);

        Assert.True(gen1 < gen2);
        Assert.True(gen2 < exo1);
        Assert.True(exo1 > gen1);
    }

    [Fact]
    public void InvalidBookId_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new VerseReference((BookId)99, 1, 1));
    }

    [Fact]
    public void InvalidChapter_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new VerseReference(BookId.Genesis, 0, 1));
    }

    [Fact]
    public void NegativeVerse_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new VerseReference(BookId.Genesis, 1, -1));
    }
}
