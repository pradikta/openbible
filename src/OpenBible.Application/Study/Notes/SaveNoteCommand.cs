using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Enums;

namespace OpenBible.Application.Study.Notes;

/// <summary>Saves a verse note. Blank text removes the note. Returns true when a note is stored.</summary>
public sealed record SaveNoteCommand(string VersionId, BookId BookId, int Chapter, int Verse, string Text) : IRequest<Result<bool>>;
