using MediatR;
using OpenBible.Application.BibleVersions.DTOs;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Enums;

namespace OpenBible.Application.BibleVersions.Commands.ImportYesFile;

public sealed record ImportYesFileCommand(
    Stream FileStream,
    string FileName,
    IProgress<ImportProgress>? Progress = null,
    BibleFormat Format = BibleFormat.Yes,
    string? SourceId = null,
    string? License = null
) : IRequest<Result<BibleVersionDto>>;
