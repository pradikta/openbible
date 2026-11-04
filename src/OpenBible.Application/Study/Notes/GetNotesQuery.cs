using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.Study.DTOs;

namespace OpenBible.Application.Study.Notes;

public sealed record GetNotesQuery(string VersionId) : IRequest<Result<IReadOnlyList<NoteDto>>>;
