using MediatR;
using OpenBible.Application.BibleVersions.DTOs;
using OpenBible.Application.Common.Models;

namespace OpenBible.Application.BibleVersions.Queries.GetInstalledVersions;

public sealed record GetInstalledVersionsQuery : IRequest<Result<IReadOnlyList<BibleVersionDto>>>;
