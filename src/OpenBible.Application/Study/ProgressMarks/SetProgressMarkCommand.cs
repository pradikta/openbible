using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Enums;

namespace OpenBible.Application.Study.ProgressMarks;

public sealed record SetProgressMarkCommand(int Slot, string VersionId, BookId BookId, int Chapter, int Verse) : IRequest<Result>;
