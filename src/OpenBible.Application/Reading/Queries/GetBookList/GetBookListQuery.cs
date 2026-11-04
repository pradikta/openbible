using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.Reading.DTOs;

namespace OpenBible.Application.Reading.Queries.GetBookList;

public sealed record GetBookListQuery : IRequest<Result<IReadOnlyList<BookListItemDto>>>;
