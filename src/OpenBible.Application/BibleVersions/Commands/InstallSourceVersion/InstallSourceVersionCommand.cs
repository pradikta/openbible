using MediatR;
using OpenBible.Application.BibleVersions.DTOs;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Enums;

namespace OpenBible.Application.BibleVersions.Commands.InstallSourceVersion;

public sealed record InstallSourceVersionCommand(
    string SourceId,
    string TranslationId,
    BibleFormat Format,
    string? License = null) : IRequest<Result<BibleVersionDto>>;
