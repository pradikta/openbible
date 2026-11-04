using FluentValidation;

namespace OpenBible.Application.Reading.Queries.GetChapter;

public sealed class GetChapterValidator : AbstractValidator<GetChapterQuery>
{
    public GetChapterValidator()
    {
        RuleFor(x => x.VersionId)
            .NotEmpty().WithMessage("Version ID cannot be empty.");

        RuleFor(x => x.BookId)
            .IsInEnum().WithMessage("BookId must be a valid biblical book.");

        RuleFor(x => x.Chapter)
            .GreaterThan(0).WithMessage("Chapter number must be greater than zero.");
    }
}
