using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Enums;

namespace OpenBible.Application.UserData.Commands.SetVerseHighlight;

public sealed record SetVerseHighlightCommand(
    string VersionId,
    BookId BookId,
    int Chapter,
    int Verse,
    HighlightColor Color) : IRequest<Result<bool>>;
