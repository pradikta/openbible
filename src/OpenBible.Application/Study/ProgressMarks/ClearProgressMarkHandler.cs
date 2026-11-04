using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.Study.ProgressMarks;

public sealed class ClearProgressMarkHandler(IStudyDataRepository study) : IRequestHandler<ClearProgressMarkCommand, Result>
{
    public async Task<Result> Handle(ClearProgressMarkCommand request, CancellationToken ct)
    {
        await study.ClearProgressMarkAsync(request.Slot, ct).ConfigureAwait(false);
        return Result.Success();
    }
}
