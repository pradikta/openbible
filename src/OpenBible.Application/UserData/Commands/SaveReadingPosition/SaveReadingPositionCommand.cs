using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Enums;

namespace OpenBible.Application.UserData.Commands.SaveReadingPosition;

public sealed record SaveReadingPositionCommand(
    string VersionId,
    BookId BookId,
    int Chapter,
    int Verse) : IRequest<Result>;
