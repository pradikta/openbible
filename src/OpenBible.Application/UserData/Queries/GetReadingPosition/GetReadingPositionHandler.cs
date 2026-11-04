using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.UserData.DTOs;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.UserData.Queries.GetReadingPosition;

public sealed class GetReadingPositionHandler : IRequestHandler<GetReadingPositionQuery, Result<ReadingPositionDto?>>
{
    private readonly IUserDataRepository _userDataRepository;

    public GetReadingPositionHandler(IUserDataRepository userDataRepository)
    {
        _userDataRepository = userDataRepository;
    }

    public async Task<Result<ReadingPositionDto?>> Handle(GetReadingPositionQuery request, CancellationToken ct)
    {
        var position = await _userDataRepository
            .GetReadingPositionAsync(request.VersionId, ct)
            .ConfigureAwait(false);

        if (position is null)
        {
            return Result<ReadingPositionDto?>.Success(null);
        }

        var dto = new ReadingPositionDto(
            position.VersionId,
            position.Reference.Book,
            position.Reference.Chapter,
            position.Reference.Verse,
            position.Reference.ToAri(),
            position.LastReadAt);

        return Result<ReadingPositionDto?>.Success(dto);
    }
}
