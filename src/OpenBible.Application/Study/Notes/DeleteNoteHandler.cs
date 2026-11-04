using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.Study.Notes;

public sealed class DeleteNoteHandler(IStudyDataRepository study) : IRequestHandler<DeleteNoteCommand, Result>
{
    public async Task<Result> Handle(DeleteNoteCommand request, CancellationToken ct)
    {
        await study.DeleteNoteAsync(request.Id, ct).ConfigureAwait(false);
        return Result.Success();
    }
}
