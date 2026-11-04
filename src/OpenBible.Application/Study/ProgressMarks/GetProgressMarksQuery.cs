using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.Study.DTOs;

namespace OpenBible.Application.Study.ProgressMarks;

public sealed record GetProgressMarksQuery : IRequest<Result<IReadOnlyList<ProgressMarkDto>>>;
