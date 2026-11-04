using MediatR;
using OpenBible.Application.Common.Models;

namespace OpenBible.Application.Study.Labels;

public sealed record GetMarkerLabelsQuery(Guid MarkerId) : IRequest<Result<IReadOnlyList<Guid>>>;
