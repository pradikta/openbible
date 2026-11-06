using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;

namespace OpenBible.Domain.Interfaces;

/// <summary>Client-side access to the deployment-configured source registry and proxy.</summary>
public interface ISourceDirectory
{
    Task<IReadOnlyList<IBibleSource>> GetSourcesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<SourceTranslation>> GetTranslationsAsync(
        string sourceId,
        string? search = null,
        CancellationToken ct = default);

    Task<Stream> DownloadAsync(string sourceId, string translationId, CancellationToken ct = default);
}
