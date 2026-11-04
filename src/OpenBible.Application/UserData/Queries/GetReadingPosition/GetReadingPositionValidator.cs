using FluentValidation;

namespace OpenBible.Application.UserData.Queries.GetReadingPosition;

public sealed class GetReadingPositionValidator : AbstractValidator<GetReadingPositionQuery>
{
    public GetReadingPositionValidator()
    {
        RuleFor(x => x.VersionId)
            .NotEmpty().WithMessage("Version ID cannot be empty.");
    }
}
