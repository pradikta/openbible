using OpenBible.Domain.Interfaces;
using OpenBible.Server.Sources;

namespace OpenBible.Server;

internal static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/admin", () => Results.Content(AdminPage, "text/html; charset=utf-8"));
        app.MapGet("/api/admin/sources", GetSourcesAsync);
        app.MapGet("/api/admin/sources/{sourceId}/catalog", GetCatalogAsync);
        app.MapPost("/api/admin/sources/{sourceId}/refresh", RefreshAsync);
        return app;
    }

    private static async Task<IResult> GetSourcesAsync(
        HttpContext context,
        IConfiguration configuration,
        ISourceRegistry registry,
        SourceCatalogService catalog,
        CancellationToken ct)
    {
        if (!IsAuthorized(context, configuration))
        {
            return Results.Unauthorized();
        }

        var result = new List<object>();
        foreach (var source in registry.Sources)
        {
            var info = await catalog.GetCacheInfoAsync(source, ct).ConfigureAwait(false);
            result.Add(new
            {
                id = source.Id,
                displayName = source.DisplayName,
                format = source.Format.ToString().ToLowerInvariant(),
                catalogKind = source.CatalogKind.ToString().ToLowerInvariant(),
                catalogFormat = source.CatalogFormat,
                host = source.Host,
                downloadHost = source.DownloadHost ?? source.Host,
                refreshMinutes = source.RefreshMinutes,
                cacheMinutes = source.CacheMinutes,
                itemCount = info?.ItemCount ?? 0,
                fetchedAt = info?.FetchedAt,
                expired = info?.Expired ?? false
            });
        }

        return Results.Json(result);
    }

    private static async Task<IResult> GetCatalogAsync(
        string sourceId,
        HttpContext context,
        IConfiguration configuration,
        ISourceRegistry registry,
        SourceCatalogService catalog,
        CancellationToken ct)
    {
        if (!IsAuthorized(context, configuration))
        {
            return Results.Unauthorized();
        }

        var source = registry.Find(sourceId);
        if (source is null)
        {
            return Results.NotFound();
        }

        var items = await catalog.GetCachedTranslationsAsync(source, ct).ConfigureAwait(false);
        return Results.Json(items.Select(item => new
        {
            id = item.Id,
            displayName = item.DisplayName,
            language = item.Language,
            description = item.Description,
            license = item.License,
            downloadUrl = item.DownloadUrl
        }));
    }

    private static async Task<IResult> RefreshAsync(
        string sourceId,
        HttpContext context,
        IConfiguration configuration,
        ISourceRegistry registry,
        SourceCatalogService catalog,
        CancellationToken ct)
    {
        if (!IsAuthorized(context, configuration))
        {
            return Results.Unauthorized();
        }

        var source = registry.Find(sourceId);
        if (source is null)
        {
            return Results.NotFound();
        }

        try
        {
            await catalog.RefreshAsync(source, ct).ConfigureAwait(false);
            var info = await catalog.GetCacheInfoAsync(source, ct).ConfigureAwait(false);
            return Results.Json(new { count = info?.ItemCount ?? 0, fetchedAt = info?.FetchedAt });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private static bool IsAuthorized(HttpContext context, IConfiguration configuration)
    {
        var expected = Environment.GetEnvironmentVariable("OPENBIBLE_ADMIN_TOKEN")
            ?? configuration["Admin:Token"];
        if (string.IsNullOrWhiteSpace(expected))
        {
            return true;
        }

        var provided = context.Request.Headers["X-Admin-Token"].ToString();
        if (string.IsNullOrEmpty(provided))
        {
            provided = context.Request.Query["token"].ToString();
        }

        return string.Equals(provided, expected, StringComparison.Ordinal);
    }

    private const string AdminPage = """
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>OpenBible admin · catalogs</title>
          <style>
            :root { color-scheme: light dark; --bg:#f7f5f2; --surface:#fffdf9; --text:#1c1917; --muted:#78716c; --line:#e7e5e4; --accent:#1e3a5f; --danger:#991b1b; }
            @media (prefers-color-scheme: dark) { :root { --bg:#141210; --surface:#1c1917; --text:#f5f5f4; --muted:#a8a29e; --line:#44403c; --accent:#93c5fd; --danger:#fecaca; } }
            * { box-sizing: border-box; }
            body { margin:0; padding:1.25rem; font:15px/1.5 ui-sans-serif,system-ui,-apple-system,Segoe UI,Roboto,sans-serif; background:var(--bg); color:var(--text); }
            main { max-width: 1100px; margin: 0 auto; }
            h1 { font-size: 1.35rem; margin: 0 0 .25rem; letter-spacing: -.02em; }
            h2 { font-size: 1.05rem; margin: 0 0 .75rem; }
            .muted { color: var(--muted); font-size: .875rem; }
            .panel { background: var(--surface); border:1px solid var(--line); border-radius: .9rem; padding:1rem; margin-top:1rem; }
            .toolbar { display:flex; flex-wrap:wrap; align-items:end; gap:.75rem; margin-top:1rem; }
            label { display:flex; flex-direction:column; gap:.3rem; font-size:.8rem; color:var(--muted); }
            input { padding:.5rem .65rem; border-radius:.5rem; border:1px solid var(--line); background:var(--surface); color:var(--text); min-width: 240px; }
            button { cursor:pointer; border:1px solid var(--line); background:var(--surface); color:var(--text); border-radius:9999px; padding:.45rem 1rem; font-size:.85rem; }
            button:hover { border-color: var(--accent); }
            button.primary { background: var(--accent); color: #f8fafc; border-color: transparent; }
            button:disabled { opacity:.5; cursor: wait; }
            table { width:100%; border-collapse: collapse; font-size:.85rem; }
            th, td { text-align:left; padding:.55rem .5rem; border-bottom:1px solid var(--line); vertical-align: top; }
            td.description { max-width: 32rem; white-space: pre-wrap; }
            th { color: var(--muted); font-weight:600; font-size:.75rem; text-transform: uppercase; letter-spacing:.04em; }
            tr:last-child td { border-bottom:0; }
            .badge { display:inline-block; border-radius:9999px; padding:.1rem .5rem; font-size:.7rem; background:var(--line); color:var(--muted); }
            .badge.stale { background:#fef3c7; color:#92400e; }
            .error { color: var(--danger); }
            a { color: var(--accent); word-break: break-all; }
            code { font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace; font-size:.8rem; }
          </style>
        </head>
        <body>
        <main>
          <h1>Catalog admin</h1>
          <p class="muted">Inspect cached source catalogs and refresh them from upstream.</p>
          <section class="toolbar">
            <label>Admin token
              <input id="token" type="password" autocomplete="off" placeholder="required when OPENBIBLE_ADMIN_TOKEN is set">
            </label>
            <button id="load" class="primary">Load sources</button>
            <span id="status" class="muted"></span>
          </section>
          <section class="panel">
            <h2>Sources</h2>
            <table>
              <thead><tr><th>Source</th><th>Format</th><th>Catalog</th><th>Items</th><th>Fetched</th><th></th></tr></thead>
              <tbody id="sources"></tbody>
            </table>
          </section>
          <section class="panel" id="catalogPanel" hidden>
            <h2 id="catalogTitle">Catalog</h2>
            <table>
              <thead><tr><th>ID</th><th>Name</th><th>Language</th><th>Description</th><th>License</th><th>Download</th></tr></thead>
              <tbody id="catalog"></tbody>
            </table>
          </section>
        </main>
        <script>
          const tokenInput = document.getElementById('token');
          const status = document.getElementById('status');
          const sourcesBody = document.getElementById('sources');
          const catalogPanel = document.getElementById('catalogPanel');
          const catalogTitle = document.getElementById('catalogTitle');
          const catalogBody = document.getElementById('catalog');
          tokenInput.value = sessionStorage.getItem('openbible.adminToken') || '';
          tokenInput.addEventListener('change', () => sessionStorage.setItem('openbible.adminToken', tokenInput.value.trim()));

          function setStatus(text, isError = false) {
            status.textContent = text;
            status.className = isError ? 'error' : 'muted';
          }

          async function api(path, options = {}) {
            const headers = Object.assign({}, options.headers || {});
            const token = tokenInput.value.trim();
            if (token) headers['X-Admin-Token'] = token;
            const response = await fetch(path, Object.assign({}, options, { headers }));
            if (response.status === 401) throw new Error('Unauthorized. Enter the admin token.');
            if (!response.ok) {
              let message = response.status + ' ' + response.statusText;
              try { const body = await response.json(); if (body.error) message = body.error; } catch {}
              throw new Error(message);
            }
            return response.json();
          }

          function cell(text, className) {
            const td = document.createElement('td');
            if (className) td.className = className;
            td.textContent = text ?? '';
            return td;
          }

          function badge(text, stale) {
            const span = document.createElement('span');
            span.className = 'badge' + (stale ? ' stale' : '');
            span.textContent = text;
            return span;
          }

          async function loadSources() {
            setStatus('Loading…');
            try {
              const sources = await api('/api/admin/sources');
              sourcesBody.replaceChildren();
              for (const source of sources) {
                const tr = document.createElement('tr');
                const name = document.createElement('td');
                const strong = document.createElement('strong');
                strong.textContent = source.displayName;
                name.append(strong, document.createElement('br'), Object.assign(document.createElement('span'), { className: 'muted', textContent: source.id }));
                tr.append(name, cell(source.format), cell(source.catalogKind + ' / ' + (source.catalogFormat || 'catalog-json')), cell(String(source.itemCount)));
                const fetched = source.fetchedAt ? new Date(source.fetchedAt).toLocaleString() : 'never';
                const fetchedCell = document.createElement('td');
                fetchedCell.append(document.createTextNode(fetched + ' '));
                if (source.expired) fetchedCell.append(badge('stale', true));
                tr.append(fetchedCell);
                const actions = document.createElement('td');
                const refresh = document.createElement('button');
                refresh.textContent = 'Refresh';
                refresh.onclick = () => refreshSource(source.id, source.displayName, refresh);
                const view = document.createElement('button');
                view.textContent = 'View';
                view.onclick = () => showCatalog(source.id, source.displayName);
                actions.append(refresh, document.createTextNode(' '), view);
                tr.append(actions);
                sourcesBody.append(tr);
              }
              setStatus(sources.length + ' source(s)');
            } catch (error) {
              setStatus(error.message, true);
            }
          }

          async function refreshSource(id, name, button) {
            button.disabled = true;
            setStatus('Refreshing ' + name + '…');
            try {
              const result = await api('/api/admin/sources/' + encodeURIComponent(id) + '/refresh', { method: 'POST' });
              setStatus('Refreshed ' + name + ': ' + result.count + ' items');
              await loadSources();
              await showCatalog(id, name);
            } catch (error) {
              setStatus(error.message, true);
            } finally {
              button.disabled = false;
            }
          }

          async function showCatalog(id, name) {
            setStatus('Loading catalog for ' + name + '…');
            try {
              const items = await api('/api/admin/sources/' + encodeURIComponent(id) + '/catalog');
              catalogTitle.textContent = name + ' · ' + items.length + ' items';
              catalogBody.replaceChildren();
              for (const item of items) {
                const tr = document.createElement('tr');
                tr.append(cell(item.id), cell(item.displayName), cell(item.language), cell(item.description, 'description'), cell(item.license || 'not provided'));
                const linkCell = document.createElement('td');
                const link = document.createElement('a');
                link.href = item.downloadUrl;
                link.target = '_blank';
                link.rel = 'noopener noreferrer';
                link.textContent = item.downloadUrl;
                linkCell.append(link);
                tr.append(linkCell);
                catalogBody.append(tr);
              }
              catalogPanel.hidden = false;
              setStatus(items.length + ' cached item(s)');
            } catch (error) {
              setStatus(error.message, true);
            }
          }

          document.getElementById('load').onclick = loadSources;
          loadSources();
        </script>
        </body>
        </html>
        """;
}
