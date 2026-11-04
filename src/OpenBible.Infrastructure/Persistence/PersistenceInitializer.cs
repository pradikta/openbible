using OpenBible.Domain.Interfaces;

namespace OpenBible.Infrastructure.Persistence;

/// <summary>Restores persisted versions, search indexes and user data once at startup.</summary>
public sealed class PersistenceInitializer(
    PersistentBibleRepository bibleRepository,
    PersistentUserDataRepository userDataRepository,
    ISearchIndex searchIndex)
{
    public async Task RestoreAsync(CancellationToken ct = default)
    {
        await bibleRepository.RestoreAsync(searchIndex, ct).ConfigureAwait(false);
        await userDataRepository.RestoreAsync(ct).ConfigureAwait(false);
    }
}
