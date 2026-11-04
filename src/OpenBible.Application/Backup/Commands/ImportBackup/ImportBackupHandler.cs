using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.Backup.Commands.ImportBackup;

public sealed class ImportBackupHandler(IBackupService backup) : IRequestHandler<ImportBackupCommand, Result>
{
    public async Task<Result> Handle(ImportBackupCommand request, CancellationToken ct)
    {
        try
        {
            await backup.ImportAsync(request.Json, ct).ConfigureAwait(false);
            return Result.Success();
        }
        catch (InvalidDataException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
