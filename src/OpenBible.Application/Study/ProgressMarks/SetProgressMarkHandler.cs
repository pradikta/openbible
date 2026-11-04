using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Interfaces;
using OpenBible.Domain.ValueObjects;

namespace OpenBible.Application.Study.ProgressMarks;

public sealed class SetProgressMarkHandler(IStudyDataRepository study) : IRequestHandler<SetProgressMarkCommand, Result>
{
    public async Task<Result> Handle(SetProgressMarkCommand request, CancellationToken ct)
    {
        var reference = new VerseReference(request.BookId, request.Chapter, request.Verse);
        await study.SaveProgressMarkAsync(new ProgressMark(request.Slot, request.VersionId, reference, DateTimeOffset.UtcNow), ct)
            .ConfigureAwait(false);
        return Result.Success();
    }
}
