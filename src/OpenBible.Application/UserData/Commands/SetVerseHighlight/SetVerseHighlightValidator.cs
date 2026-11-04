using FluentValidation;

namespace OpenBible.Application.UserData.Commands.SetVerseHighlight;

public sealed class SetVerseHighlightValidator : AbstractValidator<SetVerseHighlightCommand>
{
    public SetVerseHighlightValidator()
    {
        RuleFor(x => x.VersionId)
            .NotEmpty().WithMessage("Version ID cannot be empty.");

        RuleFor(x => x.BookId)
            .IsInEnum().WithMessage("BookId must be a valid biblical book.");

        RuleFor(x => x.Chapter)
            .GreaterThan(0).WithMessage("Chapter number must be greater than zero.");

        RuleFor(x => x.Verse)
            .GreaterThan(0).WithMessage("Verse number must be greater than zero.");

        RuleFor(x => x.Color)
            .IsInEnum().WithMessage("Highlight color is not valid.");
    }
}
