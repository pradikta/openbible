using MediatR;
using OpenBible.Application.Backup.DTOs;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.Backup.Queries.ExportBackup;

public sealed class ExportBackupHandler(IBackupService backup, TimeProvider clock)
    : IRequestHandler<ExportBackupQuery, Result<BackupFileDto>>
{
    public async Task<Result<BackupFileDto>> Handle(ExportBackupQuery request, CancellationToken ct)
    {
        var content = await backup.ExportAsync(ct).ConfigureAwait(false);
        var fileName = $"openbible-backup-{clock.GetUtcNow():yyyyMMdd-HHmmss}.json";
        return Result<BackupFileDto>.Success(new BackupFileDto(fileName, content));
    }
}
