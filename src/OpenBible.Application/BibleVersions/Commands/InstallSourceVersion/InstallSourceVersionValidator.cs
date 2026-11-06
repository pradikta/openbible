using FluentValidation;

namespace OpenBible.Application.BibleVersions.Commands.InstallSourceVersion;

public sealed class InstallSourceVersionValidator : AbstractValidator<InstallSourceVersionCommand>
{
    public InstallSourceVersionValidator()
    {
        RuleFor(x => x.SourceId).NotEmpty();
        RuleFor(x => x.TranslationId).NotEmpty();
        RuleFor(x => x.Format).IsInEnum();
        RuleFor(x => x.License).MaximumLength(4000);
    }
}
