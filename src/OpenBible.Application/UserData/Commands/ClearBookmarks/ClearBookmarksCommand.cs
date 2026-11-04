using MediatR;
using OpenBible.Application.Common.Models;

namespace OpenBible.Application.UserData.Commands.ClearBookmarks;

public sealed record ClearBookmarksCommand(string VersionId) : IRequest<Result>;
