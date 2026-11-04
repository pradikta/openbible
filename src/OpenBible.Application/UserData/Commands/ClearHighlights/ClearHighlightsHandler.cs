using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.UserData.Commands.ClearHighlights;

public sealed class ClearHighlightsHandler : IRequestHandler<ClearHighlightsCommand, Result>
{
    private readonly IUserDataRepository _userDataRepository;

    public ClearHighlightsHandler(IUserDataRepository userDataRepository)
    {
        _userDataRepository = userDataRepository;
    }

    public async Task<Result> Handle(ClearHighlightsCommand request, CancellationToken ct)
    {
        await _userDataRepository.ClearHighlightsAsync(request.VersionId, ct).ConfigureAwait(false);
        return Result.Success();
    }
}
