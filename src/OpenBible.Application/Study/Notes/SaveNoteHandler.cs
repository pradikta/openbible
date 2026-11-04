using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Interfaces;
using OpenBible.Domain.ValueObjects;

namespace OpenBible.Application.Study.Notes;

public sealed class SaveNoteHandler(IStudyDataRepository study) : IRequestHandler<SaveNoteCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(SaveNoteCommand request, CancellationToken ct)
    {
        var reference = new VerseReference(request.BookId, request.Chapter, request.Verse);
        var existing = (await study.GetNotesAsync(request.VersionId, ct).ConfigureAwait(false))
            .FirstOrDefault(n => n.Reference == reference);

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            if (existing is not null)
            {
                await study.DeleteNoteAsync(existing.Id, ct).ConfigureAwait(false);
            }

            return Result<bool>.Success(false);
        }

        var note = new Note(existing?.Id ?? Guid.NewGuid(), request.VersionId, reference, request.Text.Trim(), DateTimeOffset.UtcNow);
        await study.SaveNoteAsync(note, ct).ConfigureAwait(false);
        return Result<bool>.Success(true);
    }
}
