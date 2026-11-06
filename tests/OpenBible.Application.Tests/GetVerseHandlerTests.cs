using MediatR;
using NSubstitute;
using OpenBible.Application.Common.Models;
using OpenBible.Application.Reading.DTOs;
using OpenBible.Application.Reading.Queries.GetChapter;
using OpenBible.Application.Reading.Queries.GetVerse;
using OpenBible.Domain.Enums;
using Xunit;

namespace OpenBible.Application.Tests;

public sealed class GetVerseHandlerTests
{
    [Fact]
    public async Task Handle_ExistingVerse_ShouldReturnMatchingVerse()
    {
        var mediator = Substitute.For<IMediator>();
        var verses = new List<VerseDto>
        {
            new(1, "In the beginning God created the heaven and the earth.", 0x00010001),
            new(2, "And the earth was without form, and void.", 0x00010002)
        };
        mediator
            .Send(Arg.Any<GetChapterQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<ChapterDto>.Success(
                new ChapterDto(BookId.Genesis, "Genesis", 1, verses, Array.Empty<PericopeDto>())));

        var handler = new GetVerseHandler(mediator);
        var result = await handler.Handle(new GetVerseQuery("kjv", BookId.Genesis, 1, 2), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.VerseNumber);
        Assert.Equal("And the earth was without form, and void.", result.Value.Text);
    }

    [Fact]
    public async Task Handle_MissingVerse_ShouldReturnFailure()
    {
        var mediator = Substitute.For<IMediator>();
        mediator
            .Send(Arg.Any<GetChapterQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<ChapterDto>.Success(
                new ChapterDto(BookId.Genesis, "Genesis", 1, [], Array.Empty<PericopeDto>())));

        var handler = new GetVerseHandler(mediator);
        var result = await handler.Handle(new GetVerseQuery("kjv", BookId.Genesis, 1, 1), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_ChapterFailure_ShouldPropagateError()
    {
        var mediator = Substitute.For<IMediator>();
        mediator
            .Send(Arg.Any<GetChapterQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<ChapterDto>.Failure("Chapter not found: Genesis 999"));

        var handler = new GetVerseHandler(mediator);
        var result = await handler.Handle(new GetVerseQuery("kjv", BookId.Genesis, 999, 1), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Chapter not found: Genesis 999", result.Error);
    }
}
