using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.Study.DTOs;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.Study.Labels;

public sealed class GetLabelsHandler(IStudyDataRepository study) : IRequestHandler<GetLabelsQuery, Result<IReadOnlyList<LabelDto>>>
{
    public async Task<Result<IReadOnlyList<LabelDto>>> Handle(GetLabelsQuery request, CancellationToken ct)
    {
        var labels = await study.GetLabelsAsync(ct).ConfigureAwait(false);
        IReadOnlyList<LabelDto> dtos = [.. labels.Select(l => new LabelDto(l.Id, l.Name))];
        return Result<IReadOnlyList<LabelDto>>.Success(dtos);
    }
}
