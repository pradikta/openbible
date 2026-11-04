using MediatR;
using OpenBible.Application.Common.Models;

namespace OpenBible.Application.BibleVersions.Commands.SetDefaultVersion;

public sealed record SetDefaultVersionCommand(string VersionId) : IRequest<Result>;
