using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.UserData.DTOs;

namespace OpenBible.Application.UserData.Queries.GetBookmarks;

public sealed record GetBookmarksQuery(string VersionId) : IRequest<Result<IReadOnlyList<BookmarkDto>>>;
