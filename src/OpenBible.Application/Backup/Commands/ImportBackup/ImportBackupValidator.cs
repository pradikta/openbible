using FluentValidation;

namespace OpenBible.Application.Backup.Commands.ImportBackup;

public sealed class ImportBackupValidator : AbstractValidator<ImportBackupCommand>
{
    public ImportBackupValidator()
    {
        RuleFor(x => x.Json).NotEmpty().WithMessage("Backup file is empty.");
    }
}
