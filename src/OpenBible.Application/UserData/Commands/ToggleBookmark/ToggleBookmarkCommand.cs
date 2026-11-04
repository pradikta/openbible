using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.UserData.DTOs;
using OpenBible.Domain.Enums;

namespace OpenBible.Application.UserData.Commands.ToggleBookmark;

public sealed record ToggleBookmarkCommand(
    string VersionId,
    BookId BookId,
    int Chapter,
    int Verse) : IRequest<Result<ToggleBookmarkResultDto>>;
