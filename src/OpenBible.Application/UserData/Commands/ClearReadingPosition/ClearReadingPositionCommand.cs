using MediatR;
using OpenBible.Application.Common.Models;

namespace OpenBible.Application.UserData.Commands.ClearReadingPosition;

public sealed record ClearReadingPositionCommand(string VersionId) : IRequest<Result>;
