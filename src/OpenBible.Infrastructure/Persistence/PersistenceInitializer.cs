using OpenBible.Domain.Interfaces;

namespace OpenBible.Infrastructure.Persistence;

/// <summary>Restores persisted versions, search indexes, user data and study data once at startup.</summary>
public sealed class PersistenceInitializer(
    PersistentBibleRepository bibleRepository,
    PersistentUserDataRepository userDataRepository,
    PersistentStudyDataRepository studyDataRepository,
    PersistentAppSettingsRepository appSettingsRepository,
    ISearchIndex searchIndex)
{
    public async Task RestoreAsync(CancellationToken ct = default)
    {
        await bibleRepository.RestoreAsync(searchIndex, ct).ConfigureAwait(false);
        await userDataRepository.RestoreAsync(ct).ConfigureAwait(false);
        await studyDataRepository.RestoreAsync(ct).ConfigureAwait(false);
        await appSettingsRepository.RestoreAsync(ct).ConfigureAwait(false);
    }
}