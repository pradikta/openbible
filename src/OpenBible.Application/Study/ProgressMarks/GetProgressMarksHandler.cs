using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.Study.DTOs;
using OpenBible.Domain.Catalog;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.Study.ProgressMarks;

public sealed class GetProgressMarksHandler(IStudyDataRepository study) : IRequestHandler<GetProgressMarksQuery, Result<IReadOnlyList<ProgressMarkDto>>>
{
    public async Task<Result<IReadOnlyList<ProgressMarkDto>>> Handle(GetProgressMarksQuery request, CancellationToken ct)
    {
        var marks = await study.GetProgressMarksAsync(ct).ConfigureAwait(false);
        IReadOnlyList<ProgressMarkDto> dtos = [.. marks.Select(m => new ProgressMarkDto(
            m.Slot,
            m.VersionId,
            m.Reference.Book,
            BibleBookCatalog.Get(m.Reference.Book).FullName,
            m.Reference.Chapter,
            m.Reference.Verse,
            m.SavedAt))];
        return Result<IReadOnlyList<ProgressMarkDto>>.Success(dtos);
    }
}
