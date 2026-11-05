# ADR 0004: Multi-source, multi-format Bible content with no embedded translations

- Status: Proposed
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
2. **The source registry lives in deployment configuration.** The operator supplies a short list of hosts; each entry represents one format and one way to browse or fetch content. It contains no translation metadata. The repository only contains `sources.example.json` as documentation, and it is not loaded by default.
3. **Read content through format-neutral abstractions.** Every format is normalized into the existing Domain model (`BibleVersion`, `Book`, `Verse`, `Pericope`, `FootnoteEntry`) before reaching `IBibleRepository`. UI, search, and study features stay format-agnostic.
4. **Show rights information before installing,** where the source provides it, and require the user to confirm. Record what was shown with the installed version.
5. **Keep the server a narrow proxy,** limited to registry hosts, with no format or catalog logic beyond validation and size limits.

## Proposed abstractions

| Abstraction | Layer | Responsibility |
|---|---|---|
| `IBibleSource` | Domain | Describes one registry entry: id, display name, format, base host, catalog kind, and whether it needs the proxy. |
| `ISourceCatalog` | Domain | Lists the translations a source offers and returns per-item metadata, including license text when available. Implementations: a live listing from the host, or a catalog file supplied by the operator. |
| `IBibleFormatReader` | Domain | Reads one package (stream) of one format and yields normalized content. `YesFileParser` becomes one implementation; `IYesFileReader` and `IYesSectionReader` stay as its internals. |
| `IBibleFormatReaderSelector` | Application | Chooses the reader by declared format and, for manual import, by file signature. |
| `ISourceRegistry` | Server / Infrastructure | Loads the registry from deployment configuration and exposes it to the app. |

Application handlers (`ImportVersionHandler`, `GetSourcesQuery`, `GetSourceCatalogQuery`, `InstallVersionCommand`) stay behind MediatR and return DTOs. Blazor components still inject `IMediator` only.

## Source registry (deployment configuration)

The registry is read by `OpenBible.Server` from standard ASP.NET Core configuration: `appsettings.json`, environment variables (for example `OPENBIBLE_SOURCES__0__ID`), or a mounted JSON file whose path is set by an environment variable. The WASM client gets the list from a small endpoint (for example `GET /api/sources`) instead of an embedded file. Example shape (`sources.example.json`, not active by default):

```json
[
  { "id": "yes-example",   "format": "yes",   "host": "<host>", "catalog": { "kind": "file", "path": "/config/yes-catalog.json" } },
  { "id": "sword-example", "format": "sword", "host": "<host>", "catalog": { "kind": "live" } },
  { "id": "usfm-example",  "format": "usfm",  "host": "<host>", "catalog": { "kind": "live" } }
]
```

- **Catalog kinds:** `live` queries the host at runtime. `file` reads a catalog supplied by the operator and mounted next to the registry. This is where the former embedded Yuku `version_config.json` goes: it is no longer in the repository, and an operator who wants it provides it as a `file` catalog and accepts responsibility for its content.
- The 66-book list (`BibleBookCatalog`) is part of the ARI model and stays in the Domain layer. It is not a translation list.
- Registry entries change without a release or an image rebuild (environment variables or a mounted file in Docker Compose and Portainer).
- The server download allow-list is derived from the registry. The hard-coded host list is removed in the same milestone so there is one source of truth.
- With an empty registry the app still works: the source browser is hidden and manual import is available.
- **Static hosting without the server:** there is no registry endpoint, so only manual import is available. An optional static `sources.json` served by the host is possible, but downloads from third-party hosts still need the proxy because of CORS.

### Proxy security

Because hosts now come from configuration, the proxy must validate every request:

- HTTPS only, and the host must match a registry entry.
- Reject private, loopback, and link-local addresses (SSRF), including after DNS resolution and after each redirect.
- Re-check the host of every redirect target against the registry.
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
