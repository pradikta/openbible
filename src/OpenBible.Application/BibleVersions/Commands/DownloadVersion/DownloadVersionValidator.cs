using FluentValidation;

namespace OpenBible.Application.BibleVersions.Commands.DownloadVersion;

public sealed class DownloadVersionValidator : AbstractValidator<DownloadVersionCommand>
{
    public DownloadVersionValidator()
    {
        RuleFor(x => x.PresetName).NotEmpty().WithMessage("Preset name cannot be empty.");
    }
}
