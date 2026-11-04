using FluentValidation;

namespace OpenBible.Application.Study.Notes;

public sealed class SaveNoteValidator : AbstractValidator<SaveNoteCommand>
{
    public SaveNoteValidator()
    {
        RuleFor(x => x.VersionId).NotEmpty();
        RuleFor(x => x.BookId).IsInEnum();
        RuleFor(x => x.Chapter).GreaterThan(0);
        RuleFor(x => x.Verse).GreaterThan(0);
        RuleFor(x => x.Text).MaximumLength(4000);
    }
}
