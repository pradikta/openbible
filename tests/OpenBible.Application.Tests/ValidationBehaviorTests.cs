using FluentValidation;
using OpenBible.Application.Common.Behaviors;
using OpenBible.Application.Common.Models;
using OpenBible.Application.Reading.DTOs;
using OpenBible.Application.Reading.Queries.GetChapter;
using OpenBible.Domain.Enums;
using Xunit;

namespace OpenBible.Application.Tests;

public sealed class ValidationBehaviorTests
{
    private static ChapterDto EmptyChapter() =>
        new(BookId.Genesis, "Genesis", 1, [], Array.Empty<PericopeDto>());

    [Fact]
    public async Task Handle_InvalidRequest_ShouldReturnFailureWithoutInvokingNext()
    {
        var behavior = new ValidationBehavior<GetChapterQuery, Result<ChapterDto>>(
            new IValidator<GetChapterQuery>[] { new GetChapterValidator() });
        var nextInvoked = false;

        var response = await behavior.Handle(
            new GetChapterQuery(string.Empty, BookId.Genesis, 0),
            () =>
            {
                nextInvoked = true;
                return Task.FromResult(Result<ChapterDto>.Success(EmptyChapter()));
            },
            CancellationToken.None);

        Assert.False(nextInvoked);
        Assert.True(response.IsFailure);
        Assert.False(string.IsNullOrWhiteSpace(response.Error));
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldInvokeNext()
    {
        var behavior = new ValidationBehavior<GetChapterQuery, Result<ChapterDto>>(
            new IValidator<GetChapterQuery>[] { new GetChapterValidator() });
        var nextInvoked = false;

        var response = await behavior.Handle(
            new GetChapterQuery("kjv", BookId.Genesis, 1),
            () =>
            {
                nextInvoked = true;
                return Task.FromResult(Result<ChapterDto>.Success(EmptyChapter()));
            },
            CancellationToken.None);

        Assert.True(nextInvoked);
        Assert.True(response.IsSuccess);
    }

    [Fact]
    public async Task Handle_NoValidators_ShouldInvokeNext()
    {
        var behavior = new ValidationBehavior<GetChapterQuery, Result<ChapterDto>>(
            Array.Empty<IValidator<GetChapterQuery>>());
        var nextInvoked = false;

        var response = await behavior.Handle(
            new GetChapterQuery(string.Empty, BookId.Genesis, 0),
            () =>
            {
                nextInvoked = true;
                return Task.FromResult(Result<ChapterDto>.Success(EmptyChapter()));
            },
            CancellationToken.None);

        Assert.True(nextInvoked);
        Assert.True(response.IsSuccess);
    }
}
