using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.UserData.Commands.ClearBookmarks;

public sealed class ClearBookmarksHandler : IRequestHandler<ClearBookmarksCommand, Result>
{
    private readonly IUserDataRepository _userDataRepository;

    public ClearBookmarksHandler(IUserDataRepository userDataRepository)
    {
        _userDataRepository = userDataRepository;
    }

    public async Task<Result> Handle(ClearBookmarksCommand request, CancellationToken ct)
    {
        await _userDataRepository.ClearBookmarksAsync(request.VersionId, ct).ConfigureAwait(false);
        return Result.Success();
    }
}
