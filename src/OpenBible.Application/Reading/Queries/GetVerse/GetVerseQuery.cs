using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.Reading.DTOs;
using OpenBible.Domain.Enums;

namespace OpenBible.Application.Reading.Queries.GetVerse;

public sealed record GetVerseQuery(
    string VersionId,
    BookId BookId,
    int Chapter,
    int Verse
) : IRequest<Result<VerseDto>>;
