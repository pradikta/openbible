using MediatR;
using OpenBible.Application.Common.Models;

namespace OpenBible.Application.BibleVersions.Commands.DeleteVersion;

public sealed record DeleteVersionCommand(string VersionId) : IRequest<Result>;
