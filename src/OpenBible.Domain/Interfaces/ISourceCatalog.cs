using OpenBible.Domain.Entities;

namespace OpenBible.Domain.Interfaces;

public interface ISourceCatalog
{
    Task<IReadOnlyList<SourceTranslation>> GetTranslationsAsync(
        IBibleSource source,
        string? search = null,
        CancellationToken ct = default);

    Task<SourceTranslation?> GetTranslationAsync(
        IBibleSource source,
        string translationId,
        CancellationToken ct = default);
}
