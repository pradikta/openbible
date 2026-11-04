using MediatR;
using OpenBible.Application.Common.Models;

namespace OpenBible.Application.Study.Notes;

public sealed record DeleteNoteCommand(Guid Id) : IRequest<Result>;
