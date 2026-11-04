using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Application.UserData.DTOs;

namespace OpenBible.Application.UserData.Queries.GetReadingPosition;

public sealed record GetReadingPositionQuery(string VersionId) : IRequest<Result<ReadingPositionDto?>>;
