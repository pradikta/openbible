using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.Study.Labels;

public sealed class GetMarkerLabelsHandler(IStudyDataRepository study) : IRequestHandler<GetMarkerLabelsQuery, Result<IReadOnlyList<Guid>>>
{
    public async Task<Result<IReadOnlyList<Guid>>> Handle(GetMarkerLabelsQuery request, CancellationToken ct) =>
        Result<IReadOnlyList<Guid>>.Success(await study.GetMarkerLabelIdsAsync(request.MarkerId, ct).ConfigureAwait(false));
}
