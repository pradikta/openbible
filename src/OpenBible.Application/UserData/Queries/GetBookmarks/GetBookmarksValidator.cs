using FluentValidation;

namespace OpenBible.Application.UserData.Queries.GetBookmarks;

public sealed class GetBookmarksValidator : AbstractValidator<GetBookmarksQuery>
{
    public GetBookmarksValidator()
    {
        RuleFor(x => x.VersionId)
            .NotEmpty().WithMessage("Version ID cannot be empty.");
    }
}
