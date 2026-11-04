using OpenBible.Application.Reading.DTOs;
using OpenBible.Application.Reading.Navigation;
using OpenBible.Domain.Enums;
using Xunit;

namespace OpenBible.Application.Tests;

public sealed class PassageReferenceParserTests
{
    private static readonly BookListItemDto[] Books =
    [
        new(BookId.Genesis, "Gen", "Genesis", 50, Testament.OldTestament),
        new(BookId.Psalms, "Ps", "Psalms", 150, Testament.OldTestament),
        new(BookId.John, "John", "John", 21, Testament.NewTestament),
        new(BookId.FirstJohn, "1 John", "1 John", 5, Testament.NewTestament)
    ];

    [Theory]
    [InlineData("John 3:16", BookId.John, 3, 16)]
    [InlineData("john 3", BookId.John, 3, 0)]
    [InlineData("1 john 2:3-5", BookId.FirstJohn, 2, 3)]
    [InlineData("ps 23", BookId.Psalms, 23, 0)]
    [InlineData("Gen", BookId.Genesis, 1, 0)]
    public void Parses(string input, BookId book, int chapter, int verse)
    {
        Assert.True(PassageReferenceParser.TryParse(input, Books, out var b, out var c, out var v));
        Assert.Equal((book, chapter, verse), (b, c, v));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Nope 1")]
    [InlineData("John 99")]
    [InlineData("3")]
    public void Rejects(string input) =>
        Assert.False(PassageReferenceParser.TryParse(input, Books, out _, out _, out _));
}
