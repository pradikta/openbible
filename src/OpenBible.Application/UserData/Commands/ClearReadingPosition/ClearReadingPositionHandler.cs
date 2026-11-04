using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.UserData.Commands.ClearReadingPosition;

public sealed class ClearReadingPositionHandler : IRequestHandler<ClearReadingPositionCommand, Result>
{
    private readonly IUserDataRepository _userDataRepository;

    public ClearReadingPositionHandler(IUserDataRepository userDataRepository)
    {
        _userDataRepository = userDataRepository;
    }

    public async Task<Result> Handle(ClearReadingPositionCommand request, CancellationToken ct)
    {
        await _userDataRepository.ClearReadingPositionAsync(request.VersionId, ct).ConfigureAwait(false);
        return Result.Success();
    }
}
