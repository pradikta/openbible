using MediatR;
using OpenBible.Application.Common.Models;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Application.BibleVersions.Commands.DeleteVersion;

public sealed class DeleteVersionHandler : IRequestHandler<DeleteVersionCommand, Result>
{
    private readonly IBibleRepository _bibleRepository;

    public DeleteVersionHandler(IBibleRepository bibleRepository)
    {
        _bibleRepository = bibleRepository;
    }

    public async Task<Result> Handle(DeleteVersionCommand request, CancellationToken ct)
    {
        var existing = await _bibleRepository.GetVersionAsync(request.VersionId, ct).ConfigureAwait(false);
        if (existing is null)
        {
            return Result.Failure("Version not found.");
        }

        await _bibleRepository.DeleteVersionAsync(request.VersionId, ct).ConfigureAwait(false);
        return Result.Success();
    }
}
