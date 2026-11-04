using MediatR;
using OpenBible.Application.Backup.DTOs;
using OpenBible.Application.Common.Models;

namespace OpenBible.Application.Backup.Queries.ExportBackup;

public sealed record ExportBackupQuery : IRequest<Result<BackupFileDto>>;
