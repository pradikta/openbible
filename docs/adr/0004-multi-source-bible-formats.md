# ADR 0004: Multi-source, multi-format Bible content with no embedded translations

- Status: Accepted (2026-11-06)
- Date: 2026-11-05
- Related: [ADR 0003](0003-yes-file-format-spec.md), [PLAN.md](../../PLAN.md) Phase 6

## Context

OpenBible currently ships content and a catalog tied to one upstream:

- A full KJV text is bundled and seeded on first run (`BundledBibleSeeder`).
- The androidbible `version_config.json` (251 presets) is embedded and read by `BundledVersionCatalog`.
- `OpenBible.Server` proxies downloads from a hard-coded allow-list of hosts and only understands `.yes`.

This couples the app to one catalog and one format, and it puts the project in the position of distributing a translation list whose rights information is mostly missing. The project is intended for personal, non-commercial use, and users are responsible for the licenses of the translations they install (see the README).

## Decision

1. **Embed no translations, no translation lists, and no hosts.** The app and Docker image ship without Bible text, without any per-translation catalog, and without a default source registry (the bundled KJV and the embedded `version_config.json` are removed).
2. **The source registry lives in deployment configuration.** The operator supplies a short list of hosts; each entry represents one format and one way to browse or fetch content. It contains no translation metadata. The repository only contains `src/OpenBible.Server/sources.example.json` as documentation, and it is not loaded by default.
3. **Read content through format-neutral abstractions.** Every format is normalized into the existing Domain model (`BibleVersion`, `Book`, `Verse`, `Pericope`, `FootnoteEntry`) before reaching `IBibleRepository`. UI, search, and study features stay format-agnostic.
4. **Show rights information before installing,** where the source provides it, and require the user to confirm. Record what was shown with the installed version.
5. **Keep the server a narrow proxy,** limited to registry hosts, with no format or catalog logic beyond validation and size limits.

## Proposed abstractions

| Abstraction | Layer | Responsibility |
|---|---|---|
| `IBibleSource` | Domain | Describes one registry entry: id, display name, format, base host, catalog kind, and download metadata. `BibleSource` is the record implementation. |
| `ISourceCatalog` | Domain / Server | Lists the translations a source offers and returns per-item metadata, including license text when available. `SourceCatalogService` handles live/file catalogs plus caching. |
| `IBibleFormatReader` | Domain / Infrastructure | Reads one package (stream) of one format and yields normalized content. `YesFileParser`, `UsfmFormatReader`, and `SwordFormatReader` implement it. |
| `IBibleFormatReaderSelector` | Domain / Infrastructure | Chooses the reader by declared format and, for manual import, by file signature. |
| `ISourceRegistry` | Domain / Server | Loads the registry from deployment configuration and exposes it to the app. `SourceRegistry` is the server implementation. |
| `ISourceDirectory` | Domain / Infrastructure | Client-side HTTP adapter for `/api/sources`, catalogs, and downloads (`HttpSourceDirectory`). |

Application handlers (`ImportYesFileCommand`/`ImportYesFileHandler` for the declared formats, `GetSourcesQuery`, `GetSourceCatalogQuery`, `InstallSourceVersionCommand`) stay behind MediatR and return DTOs. Blazor components still inject `IMediator` only.

## Source registry (deployment configuration)

The registry is read by `OpenBible.Server` from standard ASP.NET Core configuration: `appsettings.json`, environment variables (for example `OPENBIBLE_SOURCES__0__ID`), or a mounted JSON file whose path is set by an environment variable. The WASM client gets the list from a small endpoint (for example `GET /api/sources`) instead of an embedded file. Example shape (`src/OpenBible.Server/sources.example.json`, not active by default):

```json
{
  "Sources": [
    {
      "id": "yes-example",
      "displayName": "YES example",
      "format": "yes",
      "host": "files.example.org",
      "downloadHost": "files.example.org",
      "catalogKind": "file",
      "catalogPath": "/config/yes-catalog.json",
      "catalogFormat": "catalog-json",
      "downloadUrlFormat": "https://files.example.org/$TRANSLATION_ID.yes",
      "refreshMinutes": 360,
      "cacheMinutes": 360
    }
  ]
}
```

- **Catalog kinds:** `live` queries the host at runtime. `file` reads a catalog supplied by the operator and mounted next to the registry. This is where the former embedded Yuku `version_config.json` goes: it is no longer in the repository, and an operator who wants it provides it as a `file` catalog and accepts responsibility for its content.
- **Catalog formats:** `catalog-json` (default), `alkitab-version-config`, `ebible-html`, and `crosswire-bibles-html`. `downloadHost` and `downloadUrlFormat` build the final download URLs when the upstream list does not include them.
- **Cache:** live catalogs are cached in memory and in `catalog-cache/{sourceId}.json`; `refreshMinutes` schedules a background refresh, and `cacheMinutes` controls the TTL. A failed refresh keeps the stale cache.
- The 66-book list (`BibleBookCatalog`) is part of the ARI model and stays in the Domain layer. It is not a translation list.
- Registry entries change without a release or an image rebuild (environment variables or a mounted file in Docker Compose and Portainer).
- The server download allow-list is derived from the registry. The hard-coded host list is removed in the same milestone so there is one source of truth.
- With an empty registry the app still works: the source browser is hidden and manual import is available.
- **Static hosting without the server:** there is no registry endpoint, so only manual import is available. An optional static `sources.json` served by the host is possible, but downloads from third-party hosts still need the proxy because of CORS.

### Proxy security

Because hosts now come from configuration, the proxy must validate every request:

- HTTPS only, and the host must match a registry entry.
- Reject private, loopback, and link-local addresses (SSRF), including after DNS resolution.
- Do not follow redirects; catalog and download URLs must be final URLs on the registered host.
- Enforce maximum download size, response time, and allowed content types.
- Do not forward cookies or credentials to sources.

## Format notes

- **YES (`.yes`, `.yes.gz`)**: already implemented. Moves behind `IBibleFormatReader` without behavior change.
- **USFM**: markup-based, one file per book, conventions vary by translation. Start here because it is the simplest to read and many sources state their license per translation.
- **SWORD**: zText modules with several versification schemes and a `.conf` with a `DistributionLicense` field. Needs a versification mapping step (see below).
- Later formats (OSIS, etc.) add only a new reader and a registry entry.

## Versification and ARI

`VerseReference` packs book, chapter, and verse and assumes one 66-book layout. Other formats can use different book sets, verse counts, or numbering. Before SWORD support:

- Define a versification mapping to the ARI layout, and document how unmapped or extra verses are handled (skip with a warning, or store under a separate scheme).
- Do not change the ARI packing without an explicit decision. This is an **ASK FIRST** area in `AGENTS.md`.

### Recorded decisions (2026-11-06)

- **Versification:** normalize every importer to the 66-book ARI layout. Books or verses that cannot be mapped are skipped and reported; content is not stored under a parallel numbering scheme.
- **Registry examples:** `src/OpenBible.Server/sources.example.json` uses placeholder hosts only; no host is active by default.
- **Migration:** implemented. The bundled KJV, `BundledBibleSeeder`, embedded `version_config.json`, and hard-coded download hosts are removed. The app starts empty, the server exposes a configured source registry, and the client installs from it. Existing installed versions remain in IndexedDB.

## Implementation status (2026-11-07)

- Source registry, `ISourceCatalog`, format reader selector, and SSRF-protected proxy are implemented.
- YES, USFM, and SWORD readers are implemented with offline tests. USFM packages are a ZIP of `.usfm`/`.sfm` files or a single file, and unmapped book codes are skipped.
- SWORD supports zText/zText4 modules with ZIP compression and KJV versification. BZIP2, LZSS, XZ, and non-KJV versifications are rejected with a clear error rather than read incorrectly. The reader was verified against a public-domain KJV zText module (66 books, 31,102 verses, Genesis 1:1-3 text correct).
- Manual import detects YES, USFM, and SWORD ZIP signatures and routes to the declared reader.
- Install shows the source-provided license (or says it is missing), requires confirmation, and stores the shown text with the installed version.
- Live catalogs are cached in memory and on disk (`catalog-cache/{sourceId}.json`) with a configurable TTL and background refresh; a failed refresh serves the stale cache. Built-in adapters cover the Alkitab `version_config.json`, the eBible redistributable listing, and the CrossWire Bible category.
- A token-protected `/admin` page and `/api/admin/*` endpoints expose cache state, cached items, and manual refresh. `OPENBIBLE_ADMIN_TOKEN` enables authentication; when unset the endpoints are open for local/trusted use.

## Consequences

Positive:

- No translation text or list is distributed with the app, which removes the bundled-content rights question and the dependency on one catalog.
- New sources and formats are added without touching UI, search, or study code.
- Rights information can be surfaced per translation instead of inferred from free-text descriptions.

Negative / risks:

- First run has no text. The UI needs an onboarding empty state that explains how to add a translation (source browser, or manual import).
- Reading offline requires at least one translation to be installed first.
- Parsers add code and tests. Each format needs offline fixtures; no live HTTP in tests.
- Sources differ in packaging (single file, zip, many files), CORS, and size. The server must handle download, validation, and limits per source kind.
- Some sources may not publish license data. The UI must say so rather than imply a translation is free to use.
- Changes to the storage contract or `.yes` parsing structure require approval before implementation.

## Migration

- Existing users keep installed versions, including a previously seeded KJV; they are plain installed data after the change.
- Removing the bundled seed affects only new installs.
- Version IDs stay stable. Add a `Format` and `SourceId` field to stored version metadata through a snapshot-compatible schema change.

## Out of scope

- Choosing the final list of hosts per format (decided when each format is implemented).
- Legal review of any source. Responsibility for translation licenses remains with the user or operator.
