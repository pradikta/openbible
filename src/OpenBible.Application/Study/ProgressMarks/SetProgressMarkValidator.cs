using FluentValidation;
using OpenBible.Domain.Entities;

namespace OpenBible.Application.Study.ProgressMarks;

public sealed class SetProgressMarkValidator : AbstractValidator<SetProgressMarkCommand>
{
    public SetProgressMarkValidator()
    {
        RuleFor(x => x.Slot).InclusiveBetween(1, ProgressMark.SlotCount);
        RuleFor(x => x.VersionId).NotEmpty();
        RuleFor(x => x.BookId).IsInEnum();
        RuleFor(x => x.Chapter).GreaterThan(0);
        RuleFor(x => x.Verse).GreaterThan(0);
    }
}
