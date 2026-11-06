using MediatR;
using OpenBible.Application.BibleVersions.DTOs;
using OpenBible.Application.Common.Models;

namespace OpenBible.Application.BibleVersions.Queries.GetSources;

public sealed record GetSourcesQuery : IRequest<Result<IReadOnlyList<BibleSourceDto>>>;
