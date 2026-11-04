using MediatR;
using OpenBible.Application.BibleVersions.DTOs;
using OpenBible.Application.Common.Models;

namespace OpenBible.Application.BibleVersions.Queries.PreviewYesFile;

public sealed record PreviewYesFileQuery(Stream FileStream, string FileName) : IRequest<Result<BibleVersionDto>>;
