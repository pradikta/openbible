using NSubstitute;
using OpenBible.Application.Common.Models;
using OpenBible.Application.Search.DTOs;
using OpenBible.Application.Search.Queries.SearchBible;
using OpenBible.Domain.Enums;
using OpenBible.Domain.Interfaces;
using OpenBible.Domain.ValueObjects;
using Xunit;

using SearchMatch = OpenBible.Domain.Interfaces.SearchMatch;

namespace OpenBible.Application.Tests;

public sealed class SearchBibleHandlerTests
{
    private readonly ISearchIndex _searchIndex;
    private readonly IBibleRepository _bibleRepository;
    private readonly SearchBibleHandler _handler;

    public SearchBibleHandlerTests()
    {
        _searchIndex = Substitute.For<ISearchIndex>();
        _bibleRepository = Substitute.For<IBibleRepository>();
        _handler = new SearchBibleHandler(_bibleRepository, _searchIndex);
    }

    [Fact]
    public async Task Handle_EmptyQueryText_ShouldReturnFailure()
    {
        var query = new SearchBibleQuery("kjv", "   ");
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("Query text cannot be empty", result.Error);
    }

    [Fact]
    public async Task Handle_NullQueryText_ShouldReturnFailure()
    {
        var query = new SearchBibleQuery("kjv", null!);
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("Query text cannot be empty", result.Error);
    }

    [Fact]
    public async Task Handle_MatchTextHasYesMarkup_ShouldStripItFromSnippet()
    {
        _searchIndex
            .SearchAsync("kjv", "shepherds", Arg.Any<SearchScope>(), Arg.Any<CancellationToken>())
            .Returns([new SearchMatch(new VerseReference(BookId.Genesis, 46, 32), "kjv", "@@And the men @9are@7 shepherds", 1.0)]);

        var result = await _handler.Handle(new SearchBibleQuery("kjv", "shepherds"), CancellationToken.None);

        Assert.Equal("And the men are shepherds", result.Value![0].Snippet);
    }

    [Fact]
    public async Task Handle_MatchesFound_ShouldReturnMappedDtos()
    {
        var matches = new[]
        {
            new SearchMatch(
                new VerseReference(BookId.John, 3, 16),
                "kjv",
                "For God so loved the world...",
                1.0),
            new SearchMatch(
                new VerseReference(BookId.John, 3, 17),
                "kjv",
                "For God sent not his Son...",
                0.9)
        };

        _searchIndex
            .SearchAsync("kjv", "god loved", Arg.Any<SearchScope>(), Arg.Any<CancellationToken>())
            .Returns(matches);

        var query = new SearchBibleQuery("kjv", "god loved");
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal(BookId.John, result.Value[0].BookId); // John 3:16 -> ari
        Assert.Equal("John", result.Value[0].BookName);
        Assert.Equal(3, result.Value[0].Chapter);
        Assert.Equal(16, result.Value[0].Verse);
        Assert.Equal("For God so loved the world...", result.Value[0].Snippet);
        Assert.Equal(1.0, result.Value[0].Score);
    }

    [Fact]
    public async Task Handle_NoMatches_ShouldReturnEmptyList()
    {
        _searchIndex
            .SearchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SearchScope>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SearchMatch>());

        var query = new SearchBibleQuery("kjv", "nonexistent");
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_ScopeTestamentOldTestament_ShouldPassScopeToIndex()
    {
        _searchIndex
            .SearchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SearchScope>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SearchMatch>());

        var query = new SearchBibleQuery("kjv", "genesis", ScopeTestament: Testament.OldTestament);
        await _handler.Handle(query, CancellationToken.None);

        await _searchIndex.Received(1).SearchAsync(
            "kjv",
            "genesis",
            Arg.Is<SearchScope>(s => s.Testament == Testament.OldTestament),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ScopeBookGenesis_ShouldPassScopeToIndex()
    {
        _searchIndex
            .SearchAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SearchScope>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<SearchMatch>());

        var query = new SearchBibleQuery("kjv", "in the beginning", ScopeBook: BookId.Genesis);
        await _handler.Handle(query, CancellationToken.None);

        await _searchIndex.Received(1).SearchAsync(
            "kjv",
            "in the beginning",
            Arg.Is<SearchScope>(s => s.Book == BookId.Genesis),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NullSearchIndex_ShouldReturnEmptyList()
    {
        var handlerNoIndex = new SearchBibleHandler(Substitute.For<IBibleRepository>(), null);
        var query = new SearchBibleQuery("kjv", "genesis");
        var result = await handlerNoIndex.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }
}
