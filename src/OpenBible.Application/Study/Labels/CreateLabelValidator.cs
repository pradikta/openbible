using FluentValidation;

namespace OpenBible.Application.Study.Labels;

public sealed class CreateLabelValidator : AbstractValidator<CreateLabelCommand>
{
    public CreateLabelValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(60);
    }
}
