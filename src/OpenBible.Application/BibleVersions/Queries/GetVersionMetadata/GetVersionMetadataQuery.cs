using MediatR;
using OpenBible.Application.BibleVersions.DTOs;
using OpenBible.Application.Common.Models;

namespace OpenBible.Application.BibleVersions.Queries.GetVersionMetadata;

public sealed record GetVersionMetadataQuery(string VersionId) : IRequest<Result<BibleVersionDto>>;
