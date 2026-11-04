using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.Study.Labels;

public sealed class DeleteLabelHandler(IStudyDataRepository study) : IRequestHandler<DeleteLabelCommand, Result>
{
    public async Task<Result> Handle(DeleteLabelCommand request, CancellationToken ct)
    {
        await study.DeleteLabelAsync(request.Id, ct).ConfigureAwait(false);
        return Result.Success();
    }
}
