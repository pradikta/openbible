using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using OpenBible.Domain.Enums;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Interfaces;

namespace OpenBible.Wasm.IndexedDb;

/// <summary>
/// <see cref="IBibleRepository"/> implementation backed by IndexedDb in Blazor WASM.
/// Stores Bible versions as JSON in an object store named "bibles".
/// Key = versionId (string), Value = serialized BibleVersion.
/// </summary>
public sealed class IndexedDbBibleRepository : IBibleRepository
{
    private readonly IJSRuntime _jsRuntime;
    private const string DbName = "OpenBible";
    private const string StoreName = "bibles";

    public IndexedDbBibleRepository(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    private async Task EnsureStoreAsync(CancellationToken ct)
    {
        await _jsRuntime.InvokeVoidAsync
        (
            "initDB",
            DbName,
            new { version = 1, stores = new[] { new { name = StoreName, keyPath = "Id", autoIncrement = false } } }
        );
    }

    private static string SerializeBibleVersion(BibleVersion version)
    {
        var booksJson = string.Join
        (
            ",",
            version.Books.Select
            (
                b => $"{{\"BookId\":{b.BookId},\"ShortName\":\"{b.ShortName}\",\"FullName\":\"{b.FullName}\",\"ChapterCount\":{b.ChapterCount},\"VerseCounts\":{string.Join(",", b.VerseCounts)}}}"
            )
        );
        return $"{{\"Id\":\"{version.Id}\",\"Title\":\"{version.Title}\",\"Language\":\"{version.Language}\",\"Description\":\"{version.Description}\",\"Books\":[{booksJson}]}}";
    }

    private BibleVersion DeserializeBibleVersion(string json)
    {
        var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;
        var id = root.GetProperty("Id").GetString()!;
        var title = root.GetProperty("Title").GetString()!;
        var language = root.GetProperty("Language").GetString()!;
        var description = root.GetProperty("Description").GetString()!;

        var books = new List<Book>();
        if (root.TryGetProperty("Books", out var booksElem))
        {
            foreach (var b in booksElem.EnumerateArray())
            {
                var bookId = (BookId)b.GetProperty("BookId").GetInt32();
                var shortName = b.GetProperty("ShortName").GetString()!;
                var fullName = b.GetProperty("FullName").GetString()!;
                var chapterCount = b.GetProperty("ChapterCount").GetInt32();
                var verseCounts = b.GetProperty("VerseCounts").EnumerateArray().Select(v => v.GetInt32()).ToArray();
                books.Add(new Book(bookId, shortName, fullName, chapterCount, verseCounts));
            }
        }

        return new BibleVersion(id, title, language, description, books);
    }

    public async Task<IReadOnlyList<BibleVersion>> GetInstalledVersionsAsync(CancellationToken ct = default)
    {
        await EnsureStoreAsync(ct);
        // For v1, return empty; full scan needs a manifest approach in v2.
        return new List<BibleVersion>();
    }

    public async Task<BibleVersion?> GetVersionAsync(string versionId, CancellationToken ct = default)
    {
        await EnsureStoreAsync(ct);
        var json = await _jsRuntime.InvokeAsync<string>("txGet", DbName, StoreName, versionId, ct);
        if (string.IsNullOrEmpty(json)) return null;
        return DeserializeBibleVersion(json);
    }

    public async Task<IReadOnlyList<Verse>> GetChapterVersesAsync(string versionId, BookId book, int chapter, CancellationToken ct = default)
    {
        throw new NotSupportedException("IndexedDb Bible chapter verse lookup not yet implemented.");
    }

    public async Task<IReadOnlyList<Pericope>> GetChapterPericopesAsync(string versionId, BookId book, int chapter, CancellationToken ct = default)
    {
        throw new NotSupportedException("IndexedDb Bible pericope lookup not yet implemented.");
    }

    public async Task SaveVersionAsync(BibleVersion version, IAsyncEnumerable<Verse> verses, IAsyncEnumerable<Pericope>? pericopes = null, CancellationToken ct = default)
    {
        // For IndexedDb v1, we just save the version metadata; verses/pericopes streaming not yet implemented.
        // We still need to consume the enumerables to avoid "non-enumerated" warnings, but we don't store them.
        await EnsureStoreAsync(ct);
        // Consume the verses enumerable
        await foreach (var verse in verses) { }
        // Consume the pericopes enumerable if not null
        if (pericopes != null)
        {
            await foreach (var pericope in pericopes) { }
        }
        var payload = SerializeBibleVersion(version);
        await _jsRuntime.InvokeVoidAsync("txPut", DbName, StoreName, version.Id, payload, ct);
    }

    public async Task DeleteVersionAsync(string versionId, CancellationToken ct = default)
    {
        await EnsureStoreAsync(ct);
        await _jsRuntime.InvokeVoidAsync("txDelete", DbName, StoreName, versionId, ct);
    }
}