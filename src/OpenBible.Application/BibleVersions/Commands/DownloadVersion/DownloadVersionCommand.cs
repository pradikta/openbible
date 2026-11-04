using MediatR;
using OpenBible.Application.BibleVersions.DTOs;
using OpenBible.Application.Common.Models;

namespace OpenBible.Application.BibleVersions.Commands.DownloadVersion;

/// <summary>Downloads a catalog translation through the configured proxy and installs it.</summary>
public sealed record DownloadVersionCommand(string PresetName) : IRequest<Result<BibleVersionDto>>;
