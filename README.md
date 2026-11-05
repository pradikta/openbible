# OpenBible

OpenBible is an **offline-first Bible reader** built with .NET 10, Blazor WebAssembly, and the Progressive Web App (PWA) platform. It brings selected features and the `.yes` Bible format used by [androidbible (Alkitab)](https://github.com/yukuku/androidbible) to the web.

The app is designed for personal, non-commercial use. Bible translation files may have separate rights and terms; see [Personal use and content rights](#personal-use-and-content-rights).

The reader, search, and personal study data run in the browser. The app needs an initial connection to load; after installation and caching, core reading works offline. Catalog downloads require a connection.

- [Status](#status)
- [Features](#features)
- [Personal use and content rights](#personal-use-and-content-rights)
- [Get started](#get-started)
- [Using OpenBible](#using-openbible)
- [Offline use and stored data](#offline-use-and-stored-data)
- [Backup and restore](#backup-and-restore)
- [Architecture](#architecture)
- [Runtime flows](#runtime-flows)
- [Verse addressing (ARI)](#verse-addressing-ari)
- [The `.yes` file format](#the-yes-file-format)
- [Mapping from androidbible](#mapping-from-androidbible)
- [Offline and PWA design](#offline-and-pwa-design)
- [Developer guide](#developer-guide)
- [Testing](#testing)
- [CI and deployment](#ci-and-deployment)
- [Known limitations and roadmap](#known-limitations-and-roadmap)
- [Credits](#credits)

---

## Status

The solution builds with zero warnings and all tests pass (Domain 18, Application 69, Infrastructure 54; 141 in total). Implemented versus pending:

| Area | State |
|---|---|
| Domain model, ARI value object, repository contracts | Implemented |
| Application use cases (read, import, search, bookmarks, highlights, reading position, study data, catalog, backup) | Implemented |
| UI pages: Reader, Library, Import, Search, and the More tools; swipe navigation; dark mode; safe-area layout | Implemented |
| `.yes` parsing: metadata, books, verse text, `snappy-blocks`, pericopes, footnotes, xrefs, `.yes.gz` | Implemented (see [ADR 0003](docs/adr/0003-yes-file-format-spec.md)) |
| Persistence: working stores snapshotted to IndexedDB and restored at startup; the search index is rebuilt on restore | Implemented |
| Bundled full KJV (public domain) imported on first run, so the reader works out of the box | Implemented |
| Translation catalog and server-side download proxy | Implemented |
| Backup and restore of personal data | Implemented |
| CI workflow, Docker image, GHCR publishing, Portainer redeploy | Implemented |
| Import progress UI, trimming/Brotli tuning, memory profiling | Pending |
| Offline end-to-end verification on physical devices | Pending |
| Side-by-side chapter panes, downloadable plans/devotions, sync, songs | Not started |

## Features

- **Reader**: chapter view with pericope headings, previous/next chapter by swipe or buttons, book selector, and automatic resume of the last reading position.
- **Verse actions**: highlight with colors, toggle bookmark, add notes and labels, set progress marks, copy or share verse text.
- **Search**: debounced (300 ms) full-text search with a scope filter (All / Old Testament / New Testament / Current book) and keyword highlighting.
- **Study tools**: bundled sample reading plans with progress, daily devotions, verse comparison across installed translations, go-to-reference, and display settings.
- **Library (Bible versions)**: list installed versions, set the default version, list bookmarks, delete a version, open Import, and browse the bundled Alkitab catalog (251 presets) with one-click install through `OpenBible.Server`.
- **Import**: preview a `.yes` or `.yes.gz` file's metadata before importing it (64 MB limit).
- **Backup and restore** (More menu): export personal data to one versioned JSON file and restore it later; see [Backup and restore](#backup-and-restore).
- **Data management**: clear history, or run a confirmed factory reset that removes all local app data.
- **PWA polish**: iOS safe-area insets, system dark/light mode plus a manual toggle, and a service worker with a cache-first strategy for published assets.
- **Deployment**: single Docker image (web app plus catalog API) for `amd64` and `arm64`, published to GHCR.

## Personal use and content rights

OpenBible is intended for **personal, non-commercial use only**. Do not sell access to this app, offer it as a paid service, or use it for commercial purposes.

This statement describes the intended use of this project; it does not grant rights to third-party software, Bible translations, catalogs, or other content. Each translation may have its own copyright, license, and redistribution restrictions. Check and follow the applicable terms before importing, sharing, or redistributing translation files. The bundled King James Version text is public domain in the United States; public-domain status can differ by jurisdiction.

There is no `LICENSE` file in this repository. Do not assume that the upstream Alkitab project’s license or a translation’s terms automatically apply to OpenBible or to content bundled with it.

### Translation licenses are the user's responsibility

- **You are responsible** for confirming that you may download, store, use, or share every translation you install or import. This applies to catalog downloads, `.yes` files you obtain elsewhere, and any copy you host yourself.
- **The developer and contributors are not responsible** for the content, accuracy, copyright status, or licensing of any translation, and accept no liability for how users or operators use them. The software is provided as is, without warranty.
- OpenBible does not host translation files, apart from the bundled KJV. Catalog items are fetched on demand from third-party sources that are outside this project's control and may change over time.
- The catalog metadata carries little or no rights information, and OpenBible does not verify rights for any entry. The absence of a notice does not mean a translation is free to use. Check the owner or publisher of each translation (for example the Bible society, Wycliffe/SIL, or the original translator) before relying on it.
- If you host OpenBible publicly or for others, you are the operator and are responsible for the translations your instance offers. Remove or disable any catalog entries you do not have the right to distribute.
- If you are a rights holder and want a catalog entry removed, contact the upstream catalog maintainers; for this repository, open an issue.

## Get started

### Run locally

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), then run from the repository root:

```powershell
dotnet restore
dotnet run --project src/OpenBible.Server
```

Open the local URL printed by the .NET tooling. `OpenBible.Server` serves the app and catalog API together. The bundled KJV is seeded on first startup.

### Run with Docker

Docker serves the web app and its optional catalog-download API:

```powershell
docker compose up --build
```

Open `http://localhost:8080` (or the port configured by `OPENBIBLE_PORT`). For more details, see [Docker deployment](#docker-deployment).

### Install as a PWA

Open the app in a supported browser and use its **Install app** or **Add to Home Screen** action. PWA installation and offline caching require a secure context: HTTPS, or `localhost` during local development. Load the app once while online so its shell and static assets can be cached.

## Using OpenBible

1. **Read:** Open the Reader, select a book and chapter, and use the chapter controls or swipe gestures to move between chapters.
2. **Study a verse:** Select a verse to open its actions. Add a bookmark or highlight, create notes and labels, set a progress mark, or copy/share the verse.
3. **Import a translation:** Open **Import**, choose a `.yes` or `.yes.gz` file, review its metadata, then confirm. The current file-size limit is 64 MB.
4. **Search:** Enter terms and optionally limit results to the Old Testament, New Testament, or current book. Select a result to navigate to that passage.
5. **More tools:** Use the More menu for reading plans, daily devotion, verse comparison, display settings, and backup/restore.
6. **Manage data:** Library lists translations, changes the default, and provides history clearing and a confirmed factory reset. A factory reset deletes local app data.

### Translation catalog

The catalog lists bundled presets. Installing a catalog item downloads its `.yes` file through `OpenBible.Server`; the server only allows presets listed in its bundled catalog and downloads only from its configured official sources. The browser cannot download directly from those sources because they do not enable CORS.

The Docker image includes the server. If using a static host without this API, download a translation manually and import it from the Import page. Catalog installation needs internet access; reading an already installed translation does not.

## Offline use and stored data

- Scripture text and personal study data are stored locally in the browser using IndexedDB. If browser storage is unavailable, the app has an in-memory fallback; data in that fallback will not survive a reload.
- The search index is built locally and reconstructed from installed translations when the app starts.
- After the first online load and service-worker caching, reading installed translations and using local study features do not require a network connection.
- Installing a catalog translation requires internet access and the catalog API. Imported files are handled in the browser.
- Clearing site data in the browser removes locally stored translations and study data. Use **Backup & restore** to export supported personal data before clearing or moving to another device. Backups do not include Bible text or display settings.
- Offline installation and reload behavior have not yet been verified end-to-end on physical iOS or Android devices; see the [roadmap](#known-limitations-and-roadmap).

## Backup and restore

Open **More → Backup & restore** (route `/backup`).

- **Export** (**Download backup**) saves a single versioned JSON file (`format: "openbible-backup"`, `version: 1`) with a timestamp.
- **Included:** reading positions, bookmarks, highlights, notes, labels, progress marks, reading-plan progress, and the default translation.
- **Not included:** installed Bible texts and display settings. Reinstall translations from Library or Import before or after restoring.
- **Restore** replaces the current personal data with the file's contents. Take a fresh export first if you want to keep what is on the device.
- **Safety:** an invalid file, a different format, or a backup from a newer app version is rejected, and nothing is changed. If applying a file fails midway, the previous data is rolled back.

## Architecture

OpenBible follows Onion Architecture. The Blazor UI sends commands and queries through MediatR; application handlers depend on domain interfaces, and Infrastructure provides their implementations.

```mermaid
flowchart TB
    subgraph Wasm["OpenBible.Wasm (Presentation)"]
        UI["Razor pages and components<br/>inject IMediator only"]
        SW["Service worker, manifest, JS interop"]
    end
    subgraph Infra["OpenBible.Infrastructure"]
        YES["YesFileParser + section readers<br/>GZip detection, Snappy"]
        REPO["Persistent repositories<br/>(Bible, user data, study data, settings)"]
        IDX["SimpleSearchIndex<br/>(rebuilt at startup)"]
        SEED["BundledBibleSeeder"]
    end
    subgraph App["OpenBible.Application"]
        UC["Commands, Queries, Handlers"]
        PIPE["LoggingBehavior, ValidationBehavior"]
        DTO["DTOs and Result&lt;T&gt;"]
    end
    subgraph Dom["OpenBible.Domain"]
        ENT["Entities and Value Objects<br/>VerseReference (ARI), VerseRange"]
        IFC["Contracts: IBibleRepository, IYesFileReader,<br/>ISearchIndex, IUserDataRepository"]
    end
    STORE[("Browser IndexedDB<br/>memory fallback")]

    UI --> UC
    UC --> IFC
    UC --> ENT
    Infra -. implements .-> IFC
    Wasm -. registers in Program.cs .-> Infra
    Infra --> App
    REPO --> STORE
```

| Project | Responsibility |
|---|---|
| `OpenBible.Domain` | Entities, value objects, and repository/service contracts. |
| `OpenBible.Application` | MediatR use cases, validation/logging behaviors, DTOs, and result types. |
| `OpenBible.Infrastructure` | `.yes` parsing, search, local repositories, bundled content, and persistence. |
| `OpenBible.Wasm` | Blazor pages/components, PWA assets, JavaScript interop, and composition root. |
| `OpenBible.Server` | Web host and allow-listed catalog download endpoint. |

Rules enforced by convention (details in [AGENTS.md](AGENTS.md)):

1. Razor components inject `IMediator` and never repositories or parsers.
2. Handlers return DTOs or `Result<T>`; domain entities never reach `.razor` files.
3. Every I/O method is `async Task`, takes a `CancellationToken`, and non-UI layers use `ConfigureAwait(false)`.
4. No sync-over-async; `.yes` files are streamed section by section, never loaded fully into a byte array.
5. One type per file; tests never touch the network.

## Runtime flows

### Request pipeline

Every UI action becomes a MediatR request that passes through the same pipeline.

```mermaid
flowchart LR
    A["Razor component"] -->|"Mediator.Send(request)"| B["LoggingBehavior"]
    B --> C["ValidationBehavior<br/>(FluentValidation)"]
    C --> D["Handler"]
    D --> E["Domain interface<br/>(IBibleRepository, ...)"]
    E --> F["Infrastructure implementation"]
    F --> E --> D
    D -->|"Result&lt;Dto&gt;"| A
```

### Reading a chapter

```mermaid
sequenceDiagram
    actor U as User
    participant R as ReaderPage
    participant M as IMediator
    participant H as GetChapterHandler
    participant Repo as IBibleRepository
    U->>R: Open /read/{version}/{book}/{chapter}
    R->>M: GetBookListQuery
    R->>M: GetChapterQuery
    M->>H: Handle
    H->>Repo: GetChapterVersesAsync(...)
    Repo-->>H: verses and pericopes
    H-->>R: Result of ChapterDto
    R->>M: SaveReadingPositionCommand
    R-->>U: ChapterView (swipe left/right = next/previous)
```

Opening `/` queries `GetReadingPositionQuery` and redirects to the last position (or a default chapter). Routes: `/`, `/read`, `/read/{VersionId}/{Book:int}/{Chapter:int}[/{Verse:int}]`.

### Importing a `.yes` file

```mermaid
sequenceDiagram
    actor U as User
    participant I as ImportPage
    participant M as IMediator
    participant P as YesFileParser
    participant Repo as IBibleRepository
    U->>I: Select .yes or .yes.gz
    I->>I: Ensure seekable stream (copy to MemoryStream if needed, max 64 MB)
    I->>M: PreviewYesFileQuery
    M->>P: ReadMetadataAsync
    P-->>I: name, language, book count
    U->>I: Confirm
    I->>M: ImportYesFileCommand
    M->>P: ReadVersesAsync / ReadPericopesAsync
    P-->>M: verses (streamed)
    M->>Repo: Save version, books, verses
    Repo->>Repo: Snapshot to IndexedDB
    M-->>I: Result
```

### Installing from the catalog

```mermaid
sequenceDiagram
    actor U as User
    participant L as Library (VersionCatalog)
    participant M as IMediator
    participant S as OpenBible.Server
    participant H as Official catalog host
    U->>L: Tap Install
    L->>M: DownloadVersionCommand(preset)
    M->>S: GET /api/catalog/download/{preset}
    S->>S: Check preset and host allow-list
    S->>H: Download .yes file
    H-->>S: File stream
    S-->>M: Stream
    M->>M: Same flow as ImportYesFileCommand
    M-->>L: Result
```

### Search

```mermaid
sequenceDiagram
    actor U as User
    participant S as SearchPage
    participant M as IMediator
    participant Idx as ISearchIndex
    U->>S: Type query
    S->>S: Debounce 300 ms
    S->>M: SearchBibleQuery(query, scope)
    M->>Idx: SearchAsync
    Idx-->>S: SearchResultDto list
    S-->>U: SearchResultList with highlighted keywords
```

The upstream search semantics (AND over space-separated terms, substring match, case-insensitive, normalized query and text) are the reference; see the androidbible [search guide](https://github.com/yukuku/androidbible/blob/develop/docs/modules/search.md).

### Bookmarks and highlights

```mermaid
sequenceDiagram
    actor U as User
    participant V as VerseActionSheet
    participant M as IMediator
    participant R as IUserDataRepository
    participant P as Persistent store
    U->>V: Select a verse
    V->>M: SetVerseHighlightCommand / ToggleBookmarkCommand
    M->>R: Save (keyed by verse ARI)
    R->>P: Snapshot to IndexedDB
    M-->>V: Result
    V-->>U: Updated verse display
```

Copy and share use JS helpers. The Library page lists bookmarks via `GetBookmarksQuery`. Markers are keyed by the verse ARI, as in androidbible's [markers guide](https://github.com/yukuku/androidbible/blob/develop/docs/modules/markers.md).

### Backup and restore

```mermaid
flowchart LR
    A["More > Backup & restore"] -->|Export| B["ExportBackupQuery"]
    B --> C["Versioned JSON file download"]
    A -->|Import| D["ImportBackupCommand"]
    D --> E{"Valid file?"}
    E -- yes --> F["Replace user data"]
    E -- no --> G["Reject, roll back, nothing changes"]
```

## Verse addressing (ARI)

A verse is identified by a 32-bit **ARI** (Alkitab Resource Identifier):

```
ari = (bookId << 16) | (chapter << 8) | verse
```

| Field | Range | Notes |
|---|---|---|
| `bookId` | 0-65 | zero-based, Genesis = 0, Revelation = 65 |
| `chapter` | 1-based | 0 means the whole book |
| `verse` | 1-based | 0 means the whole chapter |

Example: Genesis 1 (whole chapter) is `0x000100` = `256`. All bit math is encapsulated in the `VerseReference` value object (`ToAri()` / `FromAri(int)`); Application and UI code never shifts bits.

## The `.yes` file format

`.yes` (YES2) is the single-file Bible translation format of androidbible. Key facts, based on the upstream [binary formats guide](https://github.com/yukuku/androidbible/blob/develop/docs/binary-formats.md):

```
[Header]         8 bytes, starts with 0x98 0x58
[Section index]  count + (name, offset, attributes size, content size) per section
[Sections...]    version info, book info, verse text, pericopes, xrefs, footnotes
[Footer]         1 byte
```

- Numbers are **big-endian** (Java `DataInputStream`); OpenBible reads them via `BigEndianBinaryReader`.
- Verse text is UTF-8 or ASCII, may be Snappy-compressed and is addressed per book/chapter through offset tables for random access.
- A whole file may be GZip-wrapped (`.yes.gz`); OpenBible detects the `0x1F 0x8B` magic and wraps the stream in `GZipStream`.
- Inline formatting codes (red letters, footnote markers, ...) are part of the verse text; see the upstream [text rendering guide](https://github.com/yukuku/androidbible/blob/develop/docs/text-rendering.md).

```mermaid
flowchart LR
    F["file.yes / file.yes.gz"] --> G{"gzip magic?"}
    G -- yes --> Z["GZipStream"]
    G -- no --> S["raw stream"]
    Z --> H["Header check 0x98 0x58"]
    S --> H
    H --> X["Section index"]
    X --> RF["Section readers<br/>(one per section)"]
    RF --> M["Metadata and books"]
    RF --> V["Verse text<br/>(Snappy blocks)"]
    RF --> P["Pericopes"]
    RF --> N["Footnotes and xrefs<br/>(ARIF index)"]
```

Each section has its own reader so new sections can be added without modifying the core parser (Open/Closed). Snappy blocks are decoded by a managed decoder (no native dependency), and footnotes/xrefs are attached to verses through the ARIF index. Section names follow the upstream names (`versionInfo`, `booksInfo`, `text`, `xrefs`, `footnotes`, `pericopes`). Changes to the parser or ARI logic should be discussed first. Details: [ADR 0003](docs/adr/0003-yes-file-format-spec.md).

## Mapping from androidbible

| androidbible (Android) | OpenBible (.NET 10) |
|---|---|
| `Activity` / `Fragment` | Blazor page (`Pages/ReaderPage.razor`) |
| Custom view / adapter | Blazor component (`Components/ChapterView.razor`) |
| `ViewModel` / `LiveData` | MediatR query + DTO |
| `Ari` utility | `VerseReference` value object |
| `Yes2Reader` | `YesFileParser` implementing `IYesFileReader` |
| `InternalDb` / SQLite | `IBibleRepository` / `IUserDataRepository` (in-memory working store with IndexedDB snapshots) |
| `SharedPreferences` | `IUserDataRepository` and settings repository (reading position, theme, default version) |
| Version ids `internal`, `preset/<name>`, `file/<path>` | `BibleVersion` ids in the catalog |
| `DataInputStream` (big-endian) | `BinaryPrimitives.ReadInt32BigEndian` |

Upstream references: [architecture](https://github.com/yukuku/androidbible/blob/develop/docs/architecture.md), [storage](https://github.com/yukuku/androidbible/blob/develop/docs/storage.md), [versions](https://github.com/yukuku/androidbible/blob/develop/docs/modules/versions.md).

## Offline and PWA design

- `manifest.webmanifest` with `display: standalone` and Apple meta tags in `index.html`.
- `service-worker.published.js` precaches published assets with a cache-first strategy.
- `openbible.css` uses `env(safe-area-inset-*)` and `prefers-color-scheme`; `MainLayout` adds a manual theme toggle and an `ErrorBoundary`.
- App logic and tests do not need network access; only catalog installation does.
- Offline reload and service-worker behaviour still need end-to-end verification on real devices.

```mermaid
flowchart LR
    B["Browser"] --> SW{"Service worker<br/>cache hit?"}
    SW -- yes --> C["Cached app shell and assets"]
    SW -- no --> N["Network (first load)"]
    N --> SW
    C --> APP["Blazor WASM app"]
    APP --> DB[("IndexedDB<br/>translations and study data")]
```

## Developer guide

### Project structure

```
OpenBible.sln
Directory.Build.props        shared settings (net10.0, nullable, warnings as errors)
Directory.Packages.props     central package management
global.json                  .NET SDK pin
src/
  OpenBible.Domain/          entities, value objects, contracts
  OpenBible.Application/     use cases, behaviors, DTOs, Result
  OpenBible.Infrastructure/  YesFile parser, repositories, search, seeder
  OpenBible.Wasm/            Blazor WASM host (Pages, Components, Layout, wwwroot)
  OpenBible.Server/          ASP.NET Core host and catalog download API
tests/
  OpenBible.Domain.Tests/
  OpenBible.Application.Tests/
  OpenBible.Infrastructure.Tests/
docs/adr/                    architecture decision records
tools/                       deployment smoke-test scripts
PLAN.md                      milestone checklist
AGENTS.md                    architecture rules and porting guide
```

### Build

Requires the .NET 10 SDK specified in `global.json`. Run these commands from the repository root:

```powershell
dotnet restore
dotnet build --no-incremental
```

### Styling

The UI uses Tailwind CSS v4. Generated CSS is committed, so .NET restore/build does not require Node. If you change Tailwind classes or styles, rebuild the stylesheet:

```bash
cd src/OpenBible.Wasm
npm install
npm run build:css
```

## Testing

Run the test suite after building:

```powershell
dotnet test --no-build
```

Tests use xUnit, NSubstitute, and FluentAssertions. Domain tests cover ARI math and value-object equality; Application tests mock the Domain contracts to test handlers; Infrastructure tests exercise the parser and repositories. They run offline and do not make network requests.

## CI and deployment

```mermaid
flowchart LR
    PR["Pull request / push to main"] --> CI["CI workflow"]
    CI --> B["Build + test (141 tests)"]
    CI --> C["Validate docker compose"]
    CI --> S["Smoke test amd64 + arm64"]
    B & C & S --> OK{"All green on main?"}
    OK -- yes --> D["Deploy workflow (workflow_run or manual)"]
    OK -- no --> X["Stop: nothing is published"]
    D --> P["publish: multi-arch image to GHCR"]
    P --> R["redeploy: Portainer webhook (env PRD)"]
    R --> V["Poll /version.json until the new commit is live"]
```

Pull requests run CI only; they never publish or deploy.

### CI

The [CI workflow](.github/workflows/ci.yml) (named `CI`) only verifies. It runs on pushes and pull requests to `main`: it builds and tests the solution, validates the Docker Compose file, and smoke-tests the image with [tools/smoke_test.sh](tools/smoke_test.sh) on both `amd64` and `arm64` (the ARM64 image runs under QEMU because the target Portainer host is ARM64).

### Docker deployment

One image serves the PWA and the catalog download API (`OpenBible.Server`) on port `8080`. The KJV is bundled, so a fresh container is readable offline after the first load.

```powershell
docker compose up --build        # http://localhost:8080 (set OPENBIBLE_PORT to change)
docker pull ghcr.io/pradikta/openbible:latest
docker run --detach --name openbible --publish 8080:8080 ghcr.io/pradikta/openbible:latest
```

Service workers need HTTPS except on `localhost`, so put the container behind a TLS reverse proxy for real deployments. Keep any hosted deployment within the personal, non-commercial use intended for this project, and remember that you are responsible for the translations your instance offers (see [Translation licenses](#translation-licenses-are-the-users-responsibility)).

### Images and redeploy

The [deployment workflow](.github/workflows/deploy.yml) starts through `workflow_run` once `CI` has succeeded for a push to `main`, or manually through *Run workflow*. It has two jobs:

1. `publish` pushes a multi-arch image to `ghcr.io/pradikta/openbible` (`latest` and `sha-<short>`) without attestation manifests so Portainer can pull it. The commit hash and build date are baked into the image and exposed at `/version.json`.
2. `redeploy` (environment `PRD`) POSTs to the Portainer stack webhook, then polls `PUBLIC_URL/version.json` until it reports the commit that was just built.

Pull requests never publish or deploy.

**GitHub setup.** Publishing needs nothing: it logs in to GHCR with the built-in `GITHUB_TOKEN`. The redeploy job needs the `PRD` environment (*Settings → Environments*):

| Kind | Name | Purpose |
|---|---|---|
| Secret | `PORTAINER_WEBHOOK_URL` | Stack webhook URL from Portainer (*Stacks → your stack → Webhook*). Without it the job only warns and skips the call. |
| Secret (optional) | `CF_ACCESS_CLIENT_ID`, `CF_ACCESS_CLIENT_SECRET` | Cloudflare Access service token, only if Portainer sits behind Cloudflare Access. |
| Variable (optional) | `PUBLIC_URL` | Public site URL, for example `https://bible.example.com`. Enables the revision check after redeploy. |

Other notes:

- After the first publish, set the package visibility to public under *Packages → openbible → Package settings* if Portainer should pull without credentials. A private package needs a registry login in Portainer using a personal access token with `read:packages`.
- If the package already exists from another source, grant this repository write access under *Package settings → Manage Actions access*.
- In the Portainer stack, use the image `ghcr.io/pradikta/openbible:latest`.

## Known limitations and roadmap

See [PLAN.md](PLAN.md) for the detailed checklist. Remaining work includes:

- Import progress reporting for large files.
- Release-size tuning and memory profiling during large imports.
- End-to-end offline verification on physical iOS and Android devices.
- Side-by-side chapter comparison, downloadable reading plans/devotions, cross-device sync, and songs.
- Additional use cases such as direct verse lookup and a dedicated installed-version metadata query.

## Credits

OpenBible draws on the format, behavior, and documentation of [androidbible (Alkitab)](https://github.com/yukuku/androidbible) by Yuku. The bundled King James Version text is public domain in the United States. Other translations and third-party components remain subject to their own terms, and users are responsible for complying with them.
