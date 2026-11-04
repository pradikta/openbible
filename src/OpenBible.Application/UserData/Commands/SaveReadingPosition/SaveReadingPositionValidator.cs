using FluentValidation;

namespace OpenBible.Application.UserData.Commands.SaveReadingPosition;

public sealed class SaveReadingPositionValidator : AbstractValidator<SaveReadingPositionCommand>
{
    public SaveReadingPositionValidator()
    {
        RuleFor(x => x.VersionId)
            .NotEmpty().WithMessage("Version ID cannot be empty.");

        RuleFor(x => x.BookId)
            .IsInEnum().WithMessage("BookId must be a valid biblical book.");

        RuleFor(x => x.Chapter)
            .GreaterThan(0).WithMessage("Chapter number must be greater than zero.");

        RuleFor(x => x.Verse)
            .GreaterThanOrEqualTo(0).WithMessage("Verse number cannot be negative.");
    }
}
