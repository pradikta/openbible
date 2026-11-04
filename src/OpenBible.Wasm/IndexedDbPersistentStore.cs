using Microsoft.JSInterop;
using OpenBible.Infrastructure.Persistence.Storage;

namespace OpenBible.Wasm;

/// <summary>
/// IndexedDB-backed store via JS interop. If IndexedDB is unavailable (for example private mode)
/// it degrades to an in-memory store so the app keeps working for the session.
/// </summary>
public sealed class IndexedDbPersistentStore(IJSRuntime js) : IPersistentStore
{
    private readonly InMemoryPersistentStore _fallback = new();
    private bool _degraded;

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        if (!_degraded)
        {
            try
            {
                return await js.InvokeAsync<string?>("openBibleStore.get", ct, key).ConfigureAwait(false);
            }
            catch (JSException)
            {
                _degraded = true;
            }
        }

        return await _fallback.GetAsync(key, ct).ConfigureAwait(false);
    }

    public async Task SetAsync(string key, string value, CancellationToken ct = default)
    {
        if (!_degraded)
        {
            try
            {
                await js.InvokeVoidAsync("openBibleStore.set", ct, key, value).ConfigureAwait(false);
                return;
            }
            catch (JSException)
            {
                _degraded = true;
            }
        }

        await _fallback.SetAsync(key, value, ct).ConfigureAwait(false);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        if (!_degraded)
        {
            try
            {
                await js.InvokeVoidAsync("openBibleStore.remove", ct, key).ConfigureAwait(false);
                return;
            }
            catch (JSException)
            {
                _degraded = true;
            }
        }

        await _fallback.RemoveAsync(key, ct).ConfigureAwait(false);
    }
}
