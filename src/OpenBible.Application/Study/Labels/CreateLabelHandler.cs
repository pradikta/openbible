using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.Study.DTOs;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.Study.Labels;

public sealed class CreateLabelHandler(IStudyDataRepository study) : IRequestHandler<CreateLabelCommand, Result<LabelDto>>
{
    public async Task<Result<LabelDto>> Handle(CreateLabelCommand request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        var existing = (await study.GetLabelsAsync(ct).ConfigureAwait(false))
            .FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return Result<LabelDto>.Success(new LabelDto(existing.Id, existing.Name));
        }

        var label = new Label(Guid.NewGuid(), name);
        await study.SaveLabelAsync(label, ct).ConfigureAwait(false);
        return Result<LabelDto>.Success(new LabelDto(label.Id, label.Name));
    }
}
