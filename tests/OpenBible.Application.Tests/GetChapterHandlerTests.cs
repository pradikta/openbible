using NSubstitute;
using OpenBible.Application.Reading.Queries.GetChapter;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.Interfaces;
using OpenBible.Domain.ValueObjects;
using Xunit;

namespace OpenBible.Application.Tests;

public sealed class GetChapterHandlerTests
{
    private readonly IBibleRepository _bibleRepository;
    private readonly GetChapterHandler _handler;

    public GetChapterHandlerTests()
    {
        _bibleRepository = Substitute.For<IBibleRepository>();
        _handler = new GetChapterHandler(_bibleRepository);
    }

    [Fact]
    public async Task Handle_ExistingChapter_ShouldReturnSuccessWithVerses()
    {
        var verses = new List<Verse>
        {
            new(new VerseReference(BookId.Genesis, 1, 1), "In the beginning God created the heaven and the earth."),
            new(new VerseReference(BookId.Genesis, 1, 2), "And the earth was without form, and void.")
        };

        _bibleRepository
            .GetChapterVersesAsync("kjv", BookId.Genesis, 1, Arg.Any<CancellationToken>())
            .Returns(verses);

        _bibleRepository
            .GetChapterPericopesAsync("kjv", BookId.Genesis, 1, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Pericope>());

        var query = new GetChapterQuery("kjv", BookId.Genesis, 1);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(BookId.Genesis, result.Value.BookId);
        Assert.Equal(1, result.Value.ChapterNumber);
        Assert.Equal(2, result.Value.Verses.Count);
        Assert.Equal("In the beginning God created the heaven and the earth.", result.Value.Verses[0].Text);
    }

    [Fact]
    public async Task Handle_VerseWithNotes_FormatsMarkupAndMapsNotes()
    {
        var reference = new VerseReference(BookId.Genesis, 1, 1);
        var verses = new List<Verse>
        {
            new(
                reference,
                "@@Open the first footnote @<f1@>@/.",
                [new FootnoteEntry(reference, 1, "The first note.")],
                [new XRefEntry(reference, 1, "@<to:John.1.1@>John 1:1@/")])
        };

        _bibleRepository
            .GetChapterVersesAsync("kjv", BookId.Genesis, 1, Arg.Any<CancellationToken>())
            .Returns(verses);

        _bibleRepository
            .GetChapterPericopesAsync("kjv", BookId.Genesis, 1, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Pericope>());

        var result = await _handler.Handle(new GetChapterQuery("kjv", BookId.Genesis, 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var verse = Assert.Single(result.Value!.Verses);
        Assert.Equal("Open the first footnote \u00b9.", verse.Text);
        Assert.Equal("The first note.", Assert.Single(verse.Footnotes!));
        Assert.Equal("John 1:1", Assert.Single(verse.XRefs!));
    }

    [Fact]
    public async Task Handle_NonExistentChapter_ShouldReturnFailure()
    {
        _bibleRepository
            .GetChapterVersesAsync("kjv", BookId.Genesis, 999, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Verse>());

        var query = new GetChapterQuery("kjv", BookId.Genesis, 999);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }
}
