using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.Study.DTOs;
using OpenBible.Domain.Enums;

namespace OpenBible.Application.Study.Compare;

public sealed record CompareVerseQuery(BookId BookId, int Chapter, int Verse) : IRequest<Result<IReadOnlyList<VerseComparisonDto>>>;
