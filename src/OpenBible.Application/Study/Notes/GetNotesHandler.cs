using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.Study.DTOs;
using OpenBible.Domain.Catalog;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.Study.Notes;

public sealed class GetNotesHandler(IStudyDataRepository study) : IRequestHandler<GetNotesQuery, Result<IReadOnlyList<NoteDto>>>
{
    public async Task<Result<IReadOnlyList<NoteDto>>> Handle(GetNotesQuery request, CancellationToken ct)
    {
        var notes = await study.GetNotesAsync(request.VersionId, ct).ConfigureAwait(false);
        IReadOnlyList<NoteDto> dtos = [.. notes.Select(n => new NoteDto(
            n.Id,
            n.VersionId,
            n.Reference.Book,
            BibleBookCatalog.Get(n.Reference.Book).FullName,
            n.Reference.Chapter,
            n.Reference.Verse,
            n.Text,
            n.UpdatedAt))];
        return Result<IReadOnlyList<NoteDto>>.Success(dtos);
    }
}
