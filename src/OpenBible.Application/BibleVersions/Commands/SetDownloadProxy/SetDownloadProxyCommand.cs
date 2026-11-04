using MediatR;
using OpenBible.Application.Common.Models;

namespace OpenBible.Application.BibleVersions.Commands.SetDownloadProxy;

/// <summary>Stores the proxy URL template (must contain <c>{url}</c>); null or empty clears it.</summary>
public sealed record SetDownloadProxyCommand(string? ProxyUrlTemplate) : IRequest<Result>;
