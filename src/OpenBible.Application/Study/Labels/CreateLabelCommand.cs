using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.Study.DTOs;

namespace OpenBible.Application.Study.Labels;

public sealed record CreateLabelCommand(string Name) : IRequest<Result<LabelDto>>;
