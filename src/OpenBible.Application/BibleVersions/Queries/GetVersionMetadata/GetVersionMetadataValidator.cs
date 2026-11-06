using FluentValidation;

namespace OpenBible.Application.BibleVersions.Queries.GetVersionMetadata;

public sealed class GetVersionMetadataValidator : AbstractValidator<GetVersionMetadataQuery>
{
    public GetVersionMetadataValidator()
    {
        RuleFor(x => x.VersionId)
            .NotEmpty().WithMessage("Version ID cannot be empty.");
    }
}
