using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.Study.Labels;

public sealed class SetMarkerLabelsHandler(IStudyDataRepository study) : IRequestHandler<SetMarkerLabelsCommand, Result>
{
    public async Task<Result> Handle(SetMarkerLabelsCommand request, CancellationToken ct)
    {
        var known = (await study.GetLabelsAsync(ct).ConfigureAwait(false)).Select(l => l.Id).ToHashSet();
        await study.SetMarkerLabelIdsAsync(request.MarkerId, [.. request.LabelIds.Where(known.Contains)], ct).ConfigureAwait(false);
        return Result.Success();
    }
}
