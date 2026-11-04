using MediatR;
using OpenBible.Application.Common.Models;

namespace OpenBible.Application.Study.ProgressMarks;

public sealed record ClearProgressMarkCommand(int Slot) : IRequest<Result>;
