using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.Reading.DTOs;
using OpenBible.Domain.Enums;

namespace OpenBible.Application.Reading.Queries.GetChapter;

public sealed record GetChapterQuery(
    string VersionId,
    BookId BookId,
    int Chapter
) : IRequest<Result<ChapterDto>>;
