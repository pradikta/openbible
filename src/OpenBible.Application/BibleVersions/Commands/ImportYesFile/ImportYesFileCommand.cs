using MediatR;
using OpenBible.Application.BibleVersions.DTOs;
using OpenBible.Application.Common.Models;

namespace OpenBible.Application.BibleVersions.Commands.ImportYesFile;

public sealed record ImportYesFileCommand(
    Stream FileStream,
    string FileName
) : IRequest<Result<BibleVersionDto>>;
