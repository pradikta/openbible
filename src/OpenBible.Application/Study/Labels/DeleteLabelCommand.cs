using MediatR;
using OpenBible.Application.Common.Models;

namespace OpenBible.Application.Study.Labels;

public sealed record DeleteLabelCommand(Guid Id) : IRequest<Result>;
