using System.IO.Compression;
using System.Text;

namespace OpenBible.Infrastructure.Sword;

internal sealed class SwordArchive : IDisposable
{
    private readonly ZipArchive _archive;
    private readonly Dictionary<string, ZipArchiveEntry> _entries;
    private readonly string _rootPrefix;

    private SwordArchive(
        ZipArchive archive,
        Dictionary<string, ZipArchiveEntry> entries,
        string rootPrefix,
        string confName,
        string confText)
    {
        _archive = archive;
        _entries = entries;
        _rootPrefix = rootPrefix;
        ConfName = confName;
        ConfText = confText;
    }

    public string ConfName { get; }
    public string ConfText { get; }

    public static SwordArchive Open(Stream stream)
    {
        if (!stream.CanSeek)
        {
            throw new InvalidOperationException("SWORD import requires a seekable stream.");
        }

        stream.Seek(0, SeekOrigin.Begin);
        ZipArchive archive;
        try
        {
            archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidDataException("SWORD package is not a ZIP archive.", ex);
        }

        var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
            {
                continue;
            }

            entries[Normalize(entry.FullName)] = entry;
        }

        var confEntry = entries
            .Where(pair => pair.Key.EndsWith(".conf", StringComparison.OrdinalIgnoreCase))
            .Where(pair => pair.Key.Contains("/mods.d/", StringComparison.OrdinalIgnoreCase)
                || pair.Key.StartsWith("mods.d/", StringComparison.OrdinalIgnoreCase))
            .Select(pair => pair.Value)
            .FirstOrDefault();

        if (confEntry is null)
        {
            archive.Dispose();
            throw new InvalidDataException("SWORD package does not contain a mods.d/*.conf file.");
        }

        var confPath = Normalize(confEntry.FullName);
        var marker = confPath.IndexOf("mods.d/", StringComparison.OrdinalIgnoreCase);
        var rootPrefix = marker > 0 ? confPath[..marker] : string.Empty;
        var confText = DecodeConf(ReadEntryBytes(confEntry));

        return new SwordArchive(
            archive,
            entries,
            rootPrefix,
            Path.GetFileNameWithoutExtension(confEntry.Name),
            confText);
    }

    public bool TryReadFile(string relativePath, out byte[] bytes)
    {
        var normalized = Normalize(relativePath);
        var full = Normalize(_rootPrefix + normalized);

        if (!_entries.TryGetValue(full, out var entry) && !_entries.TryGetValue(normalized, out entry))
        {
            entry = _entries
                .FirstOrDefault(pair => pair.Key.EndsWith("/" + normalized, StringComparison.OrdinalIgnoreCase))
                .Value;
        }

        if (entry is null)
        {
            bytes = [];
            return false;
        }

        bytes = ReadEntryBytes(entry);
        return true;
    }

    public void Dispose() => _archive.Dispose();

    private static byte[] ReadEntryBytes(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static string DecodeConf(byte[] bytes)
    {
        try
        {
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(bytes);
        }
    }

    private static string Normalize(string path) =>
        path.Replace('\\', '/').TrimStart('.', '/').TrimEnd('/');
}
