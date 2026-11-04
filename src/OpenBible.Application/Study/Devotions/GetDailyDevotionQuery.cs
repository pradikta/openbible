using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.Study.DTOs;

namespace OpenBible.Application.Study.Devotions;

public sealed record GetDailyDevotionQuery(DateOnly Date) : IRequest<Result<DevotionDto>>;
