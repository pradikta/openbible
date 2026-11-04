using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Interfaces;
using OpenBible.Domain.ValueObjects;

namespace OpenBible.Application.UserData.Commands.SaveReadingPosition;

public sealed class SaveReadingPositionHandler : IRequestHandler<SaveReadingPositionCommand, Result>
{
    private readonly IUserDataRepository _userDataRepository;

    public SaveReadingPositionHandler(IUserDataRepository userDataRepository)
    {
        _userDataRepository = userDataRepository;
    }

    public async Task<Result> Handle(SaveReadingPositionCommand request, CancellationToken ct)
    {
        var position = new ReadingPosition(
            request.VersionId,
            new VerseReference(request.BookId, request.Chapter, request.Verse),
            DateTimeOffset.UtcNow);

        await _userDataRepository.SaveReadingPositionAsync(position, ct).ConfigureAwait(false);
        return Result.Success();
    }
}
