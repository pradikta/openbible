namespace OpenBible.Domain.Interfaces;

/// <summary>Fetches a translation file from a catalog download URL.</summary>
public interface IVersionDownloader
{
    /// <summary>Returns a readable, seekable stream; throws <see cref="InvalidOperationException"/> with a user-facing message on failure.</summary>
    Task<Stream> DownloadAsync(string downloadUrl, CancellationToken ct = default);
}
