using FluentValidation;

namespace OpenBible.Application.BibleVersions.Commands.DeleteVersion;

public sealed class DeleteVersionValidator : AbstractValidator<DeleteVersionCommand>
{
    public DeleteVersionValidator()
    {
        RuleFor(x => x.VersionId)
            .NotEmpty().WithMessage("Version ID cannot be empty.");
    }
}
