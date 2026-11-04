using MediatR;
using OpenBible.Application.Common.Models;

namespace OpenBible.Application.Study.Labels;

/// <summary>Replaces the labels attached to a bookmark or note.</summary>
public sealed record SetMarkerLabelsCommand(Guid MarkerId, IReadOnlyList<Guid> LabelIds) : IRequest<Result>;
