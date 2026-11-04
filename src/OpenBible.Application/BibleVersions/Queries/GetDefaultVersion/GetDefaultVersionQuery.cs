using MediatR;
using OpenBible.Application.Common.Models;

namespace OpenBible.Application.BibleVersions.Queries.GetDefaultVersion;

/// <summary>Returns the default version id, or null when none is set or it is no longer installed.</summary>
public sealed record GetDefaultVersionQuery : IRequest<Result<string?>>;
