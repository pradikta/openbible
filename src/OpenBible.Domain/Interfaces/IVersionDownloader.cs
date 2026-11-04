namespace OpenBible.Domain.Interfaces;

/// <summary>Fetches a catalog translation file from the OpenBible server.</summary>
public interface IVersionDownloader
{
    /// <summary>Returns a readable, seekable stream; throws <see cref="InvalidOperationException"/> with a user-facing message on failure.</summary>
    Task<Stream> DownloadAsync(string presetName, CancellationToken ct = default);
}
