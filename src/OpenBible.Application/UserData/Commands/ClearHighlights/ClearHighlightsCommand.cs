using MediatR;
using OpenBible.Application.Common.Models;

namespace OpenBible.Application.UserData.Commands.ClearHighlights;

public sealed record ClearHighlightsCommand(string VersionId) : IRequest<Result>;
