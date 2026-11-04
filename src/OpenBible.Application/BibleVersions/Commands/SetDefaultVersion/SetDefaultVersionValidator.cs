using FluentValidation;

namespace OpenBible.Application.BibleVersions.Commands.SetDefaultVersion;

public sealed class SetDefaultVersionValidator : AbstractValidator<SetDefaultVersionCommand>
{
    public SetDefaultVersionValidator()
    {
        RuleFor(x => x.VersionId).NotEmpty().WithMessage("Version ID cannot be empty.");
    }
}
