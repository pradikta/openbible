using MediatR;
using OpenBible.Application.Common.Models;

namespace OpenBible.Application.BibleVersions.Queries.GetDownloadProxy;

public sealed record GetDownloadProxyQuery : IRequest<Result<string?>>;
