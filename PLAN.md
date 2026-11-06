# OpenBible — Master Execution Plan & Feature Checklist

> **Single Source of Truth for Project Progress**  
> Update this checklist incrementally as tasks are started `[/]` and completed `[x]`.  
> Every milestone must pass `dotnet build` and `dotnet test` with zero warnings and zero errors before proceeding.
>
> **Progress snapshot (2026-11-06):** Fresh verification passed: `dotnet build --no-incremental` succeeded with **0 warnings and 0 errors**, and `dotnet test --no-build --verbosity normal` passed all **227 tests**. Phase 0 is complete. Phase 1 is implemented except for optional domain events/exceptions. Phase 2 CQRS use cases are complete. Phase 3 parsing and IndexedDB-backed persistence are implemented and covered by tests. Phase 4 reader, library, import, search, and study pages are implemented, including import progress reporting; real-device offline verification remains. Phase 5 CI, data-management controls, and Release trimming/Brotli are implemented; memory profiling, README screenshots/usage polish, and end-to-end offline verification remain. Phase 6 ships an empty app, a deployment-configured source registry, a protected proxy, the client migration, YES/USFM/SWORD readers, manual-import format detection, and install-time license confirmation/storage; only the manual install/offline check remains.

---

## 🧭 Milestone Overview

```
Phase 0: Scaffolding ──► Phase 1: Domain Core ──► Phase 2: Application CQRS
                                                           │
Phase 5: Release ◄── Phase 4: Blazor UI & PWA ◄── Phase 3: Infra & .yes Parser
        │
        ▼
Phase 6: Multi-source, multi-format content (empty app, registry from deployment config)
```

---

## Phase 0: Solution Scaffolding & CI Baseline

- [x] **SDK & Tooling Setup**
  - [x] Create `global.json` pinning .NET 10 SDK version (`10.0.100` or latest installed).
  - [x] Create classic solution file `OpenBible.sln` via `dotnet new sln -n OpenBible --format sln`.
  - [x] Create `.gitignore` tailored for .NET 10, Blazor WASM, and IDEs.
- [x] **Centralized MSBuild Configurations**
  - [x] Create `Directory.Build.props`:
    - [x] `TargetFramework`: `net10.0`
    - [x] `Nullable`: `enable`
    - [x] `ImplicitUsings`: `enable`
    - [x] `TreatWarningsAsErrors`: `true`
  - [x] Create `Directory.Packages.props` for Central Package Management (CPM):
    - [x] Pin `MediatR` (and `MediatR.Contracts`)
    - [x] Pin `FluentValidation`
    - [x] Pin `Microsoft.AspNetCore.Components.WebAssembly`
    - [x] Pin `xunit`, `NSubstitute`
    - [x] Snappy: no package needed; managed `SnappyBlockDecoder` is implemented in Infrastructure.
    - [x] Pin IndexedDB interop package (`DnetIndexedDb` is pinned; the store uses JS interop in `wwwroot/interop/openbible-store.js`).
- [x] **Scaffold Source Projects (`src/`)**
  - [x] `src/OpenBible.Domain` (classlib, zero external dependencies)
  - [x] `src/OpenBible.Application` (classlib, references `Domain`)
  - [x] `src/OpenBible.Infrastructure` (classlib, references `Domain` + `Application`)
  - [x] `src/OpenBible.Wasm` (blazorwasm project, references `Application` + `Infrastructure`)
- [x] **Scaffold Test Projects (`tests/`)**
  - [x] `tests/OpenBible.Domain.Tests` (xunit, references `Domain`)
  - [x] `tests/OpenBible.Application.Tests` (xunit, references `Application` + `Domain`)
  - [x] `tests/OpenBible.Infrastructure.Tests` (xunit, references `Infrastructure` + `Domain` + `Application`)
- [x] **Verification Gate**
  - [x] Run `dotnet restore`
  - [x] Run `dotnet build --no-incremental` (must succeed with 0 warnings, 0 errors)
  - [x] Run `dotnet test --no-build`

---

## Phase 1: Domain Core & ARI Addressing Engine (Backend)

- [x] **Enums & Identifiers**
  - [x] Define `BookId` enum (`0` to `65`, matching androidbible zero-based index: Genesis = 0, Revelation = 65).
  - [x] Define `Testament` enum (`OldTestament`, `NewTestament`).
  - [x] Define `YesSectionType` enum (`VersionInfo`, `BookNames`, `VerseText`, `Pericope`, `Xref`, `Footnotes`).
  - [x] Define `HighlightColor` enum (`Yellow`, `Green`, `Blue`, `Pink`, `Purple`).
- [x] **Value Objects (Immutable & Structurally Equatable)**
  - [x] Implement `VerseReference`:
    - [x] `BookId Book`, `int Chapter` (1-based), `int Verse` (1-based, 0 = whole chapter).
    - [x] `int ToAri()` bit-shift packing: `(bookId << 16) | (chapter << 8) | verse`.
    - [x] `static VerseReference FromAri(int ari)` unpacking.
    - [x] `string ToDisplayString(string? bookName = null)`.
    - [x] Structural equality (`IEquatable<VerseReference>`) and operator overloads (`==`, `!=`).
  - [x] Implement `VerseRange`:
    - [x] `VerseReference Start`, `VerseReference End`.
    - [x] Range validation (same book/chapter, `Start.Verse <= End.Verse`).
    - [x] `bool Contains(VerseReference reference)`.
  - [x] Implement `YesFileDescriptor`:
    - [x] File metadata, format version (`YES1` vs `YES2`), byte length, is compressed (`.gz`).
- [x] **Domain Entities**
  - [x] `Verse`: `VerseReference Reference`, `string Text`, `IReadOnlyList<FootnoteEntry> Footnotes`, `IReadOnlyList<XRefEntry> XRefs`.
  - [x] `Chapter`: `BookId Book`, `int ChapterNumber`, `IReadOnlyList<Verse> Verses`.
  - [x] `Book`: `BookId BookId`, `string ShortName`, `string FullName`, `int ChapterCount`, `IReadOnlyList<int> VerseCounts`.
  - [x] `BibleVersion`: `string Id`, `string Title`, `string Language`, `string Description`, `IReadOnlyList<Book> Books`.
  - [x] `Pericope`: Section heading with `VerseReference Reference`, `string Title`, `IReadOnlyList<string> ParallelPassages`.
  - [x] `FootnoteEntry`: `VerseReference Reference`, `int Index`, `string Content`.
  - [x] `XRefEntry`: `VerseReference Reference`, `int Index`, `string Content`.
  - [x] `Bookmark`: `Guid Id`, `string VersionId`, `VerseReference Reference`, `DateTimeOffset CreatedAt`, `string? Note`.
  - [x] `Highlight`: `Guid Id`, `string VersionId`, `VerseReference Reference`, `HighlightColor Color`, `DateTimeOffset UpdatedAt`.
  - [x] `ReadingPosition`: `string VersionId`, `VerseReference Reference`, `DateTimeOffset LastReadAt`.
- [x] **Repository & Service Contracts (Interfaces)**
  - [x] `IBibleRepository`:
    - [x] `Task<IReadOnlyList<BibleVersion>> GetInstalledVersionsAsync(CancellationToken ct = default)`
    - [x] `Task<BibleVersion?> GetVersionAsync(string versionId, CancellationToken ct = default)`
    - [x] `Task<IReadOnlyList<Verse>> GetChapterVersesAsync(string versionId, BookId book, int chapter, CancellationToken ct = default)`
    - [x] `Task<IReadOnlyList<Pericope>> GetChapterPericopesAsync(string versionId, BookId book, int chapter, CancellationToken ct = default)`
    - [x] `Task SaveVersionAsync(BibleVersion version, IAsyncEnumerable<Verse> verses, CancellationToken ct = default)`
    - [x] `Task DeleteVersionAsync(string versionId, CancellationToken ct = default)`
  - [x] `IYesFileReader`:
    - [x] `Task<BibleVersion> ReadMetadataAsync(Stream stream, CancellationToken ct = default)`
    - [x] `IAsyncEnumerable<Verse> ReadVersesAsync(Stream stream, CancellationToken ct = default)`
    - [x] `IAsyncEnumerable<Pericope> ReadPericopesAsync(Stream stream, CancellationToken ct = default)`
  - [x] `ISearchIndex`:
    - [x] `Task IndexVersionAsync(string versionId, IAsyncEnumerable<Verse> verses, CancellationToken ct = default)`
    - [x] `Task<IReadOnlyList<SearchResult>> SearchAsync(SearchQuery query, CancellationToken ct = default)`
  - [x] `IUserDataRepository`:
    - [x] Reading position save & retrieve
    - [x] Bookmarks CRUD
    - [x] Highlights CRUD
- [/] **Domain Exceptions & Events**
  - [x] `YesFileCorruptException`, `VerseNotFoundException`.
  - [ ] `VersionAlreadyExistsException`, `BibleVersionImportedEvent`, `ReadingPositionChangedEvent`.
- [x] **Unit Tests (`OpenBible.Domain.Tests`)**
  - [x] ARI packing/unpacking roundtrip tests for boundary values (Gen 1:1, Rev 22:21, whole chapter 0x000100).
  - [x] `VerseReference` equality, comparison, and invalid input exceptions.
  - [x] `VerseRange` containment tests.
  - [x] Entity invariants and guard clauses.

---

## Phase 2: Application CQRS & Business Logic (Backend)

- [x] **Common Core & Monads**
  - [x] Implement `Result<T>` and `Result` in `Common/Models/` (Success, Failure, Error reasons).
  - [x] Pipeline Behaviors:
    - [x] `LoggingBehavior<TRequest, TResponse>` (intercepts requests, measures execution time).
    - [x] `ValidationBehavior<TRequest, TResponse>` (validates via FluentValidation, returns `Result.Failure`).
- [x] **Data Transfer Objects (DTOs)**
  - [x] `ChapterDto(BookId BookId, string BookName, int ChapterNumber, IReadOnlyList<VerseDto> Verses, IReadOnlyList<PericopeDto> Pericopes)`
  - [x] `VerseDto(int VerseNumber, string Text, int Ari, bool IsBookmarked, HighlightColor? Highlight, IReadOnlyList<string>? Footnotes, IReadOnlyList<string>? XRefs)`
  - [x] `PericopeDto(int BeforeVerse, string Title, IReadOnlyList<string> Parallels)`
  - [x] `BibleVersionDto(string Id, string Title, string Language, string Description, int TotalBooks)`
  - [x] `SearchResultDto(VerseReference Reference, string VersionId, string BookName, int Chapter, int Verse, string Snippet, double Score)`
  - [x] `BookmarkDto`, `HighlightDto`, `ReadingPositionDto`
- [x] **Use Cases: Bible Reading & Navigation**
  - [x] `GetChapterQuery` + `GetChapterHandler` + `GetChapterValidator`
  - [x] `GetVerseQuery` + `GetVerseHandler`
  - [x] `GetBookListQuery` + `GetBookListHandler` (uses installed-version books and chapter counts, falling back to the canonical catalog)
- [x] **Use Cases: Version Management & Import**
  - [x] `GetInstalledVersionsQuery` + `GetInstalledVersionsHandler`
  - [x] `GetVersionMetadataQuery` + `GetVersionMetadataHandler`
  - [x] `PreviewYesFileQuery` + handler provides import metadata preview.
  - [x] `ImportYesFileCommand` + `ImportYesFileHandler` (parses and persists imported versions)
  - [x] `DeleteVersionCommand` + `DeleteVersionHandler`
- [x] **Use Cases: Search**
  - [x] `SearchBibleQuery` + `SearchBibleHandler` (supports full-text queries, scope filtering by Testament or Book)
- [x] **Use Cases: User Data**
  - [x] `GetReadingPositionQuery` + `GetReadingPositionHandler`
  - [x] `SaveReadingPositionCommand` + `SaveReadingPositionHandler`
  - [x] `ToggleBookmarkCommand` + `ToggleBookmarkHandler`
  - [x] `SetVerseHighlightCommand` + `SetVerseHighlightHandler`
  - [x] `GetBookmarksQuery` + `GetBookmarksHandler`
  - [x] Clear reading position, bookmarks, and highlights.
- [x] **Dependency Injection Registration**
  - [x] Create `DependencyInjection.cs` with `AddApplication(this IServiceCollection)` registering MediatR, FluentValidation, and Behaviors.
- [x] **Unit Tests (`OpenBible.Application.Tests`)**
  - [x] Isolated handler tests using `NSubstitute` to mock `IBibleRepository` and `IYesFileReader`.
  - [x] Validation behavior tests (invalid query inputs return failure).

---

## Phase 3: Infrastructure Layer (YES Parser & Offline Storage)

- [x] **`.yes` Binary Format Architecture**
  - [x] Document specification in `docs/adr/0003-yes-file-format-spec.md` (layouts verified against upstream readers; local parser gaps recorded).
    - [x] `YesFileConstants`: Full YES2 signature and upstream section names (`versionInfo`, `booksInfo`, `text`, `pericopes`, `xrefs`, `footnotes`).
    - [x] Implement `BigEndianBinaryReader` wrapping `Stream` with `BinaryPrimitives.Read*BigEndian`.
- [x] **Section Readers (`Infrastructure/YesFile/Sections/`)**
  - [x] `SectionContentStream`: reads section attributes (Bintex map) and exposes section content with offsets relative to the content start.
  - [x] `SectionContentStream`: transparently decompresses `snappy-blocks` payloads for any section that declares compression.
  - [x] `MetadataSectionReader` reads title, language, description, encoding, and the declared book count from `versionInfo`; `BookNamesSectionReader` owns the ordered `booksInfo` book records.
  - [x] `BookNamesSectionReader`: parses full and short book titles (`abbreviation`/full titles are mapped from `booksInfo` to `Book.ShortName`/`Book.FullName`, replacing internal catalog names where declared).
  - [x] Verse text reader uses book/chapter offsets and VarUInt lengths for both uncompressed and `snappy-blocks` compressed sections.
  - [x] `PericopeSectionReader`: parses data format versions 2 and 3 indexes plus version 4 blocks into `Pericope` entities (title, parallel passages, untitled headings skipped).
  - [x] `ArifEntrySectionReader` plus `FootnoteSectionReader` / `XRefSectionReader`: parse the ARIF index (version 1, sorted keys, relative offsets) into `FootnoteEntry` and `XRefEntry` entities attached to their verses. Inline `@<fN@>` / `@<xN@>` markers are rendered by `Application/Reading/Formatting/VerseMarkupFormatter`.
- [x] **Decompression Engine**
  - [x] Transparent `.yes.gz` GZip stream wrapper; `PrepareStream` uses a bounded spool to provide random access.
  - [x] Managed WASM-compatible Snappy block decoder (`YesFile/Compression/SnappyBlockDecoder`, no native dependency).
  - [x] `SnappyBlockStream`: seekable view over a `snappy-blocks` container using `block_size` and `compressed_block_sizes`, decompressing one block at a time.
- [x] **Core Parser**
  - [x] Implement `YesFileParser` implementing `IYesFileReader`:
    - [x] Validates the complete YES2 signature and section index bounds.
    - [x] Reads metadata, book/chapter indexes, and verse content without loading the whole file into memory.
    - [x] Asynchronously yields verses from uncompressed and `snappy-blocks` compressed text sections.
    - [x] Asynchronously yields pericope headings from the `pericopes` section.
    - [x] Attaches `footnotes` and `xrefs` entries to the verses addressed by their ARIF keys.
    - [x] GZip random access via bounded spool (`PrepareStream`).
- [x] **Offline Storage Implementations**
  - [x] Persist installed versions: `PersistentBibleRepository` snapshots each version as JSON through `IPersistentStore` (`IndexedDbPersistentStore` in the Wasm host via JS interop; degrades to memory). Restored at startup by `PersistenceInitializer`.
  - [x] Persist user data: `PersistentUserDataRepository` snapshots reading position, bookmarks and highlights; study data and app settings have persistent repositories.
  - [x] Search index is rebuilt from persisted versions at startup (`SimpleSearchIndex` stays in-memory; no separate persisted index).
  - [x] Seed bundled Bible content on first run.
- [x] **Dependency Injection Registration**
  - [x] Create `DependencyInjection.cs` with `AddInfrastructure(this IServiceCollection)`; the host overrides `IPersistentStore`.
- [x] **Integration Tests (`OpenBible.Infrastructure.Tests`)**
  - [x] Create minimal fixture `.yes` test file in `tests/fixtures/sample.yes` (Genesis 1, 3 verses; used by `SampleYesFixtureTests` and for manual import testing).
  - [x] Test `YesFileParser` header validation and section parsing (synthetic YES2 fixtures cover index bounds, books, text, and pericopes).
  - [x] Test Snappy decompression accuracy against known sample blocks (literal, copy-1, copy-2, truncated and out-of-range blocks).
  - [x] Test `snappy-blocks` verse text: single block, payload split across blocks, unsupported compression, and corrupt blocks.
  - [x] Test pericope parsing end to end, including a compressed `pericopes` section and persistence through `InMemoryBibleRepository`.
  - [x] Test footnote and xref parsing: ARIF mapping, compressed section, missing sections, zero index, out-of-range offset, unsupported version, and persistence.
  - [x] Test Big-Endian integer reading logic.


---

## Phase 4: Presentation Layer (Blazor WASM iOS PWA UI)

- [x] **Composition Root & PWA Assets**
  - [x] `Program.cs`: Register `.AddApplication().AddInfrastructure()`.
  - [x] `manifest.webmanifest`: PWA metadata (`display: standalone`, icons, theme colors).
  - [x] `index.html`: Apple mobile tags (`apple-mobile-web-app-capable`, `apple-mobile-web-app-status-bar-style`, viewport-fit=cover).
  - [x] `service-worker.js`: Offline-first caching for WASM assemblies, CSS, and app assets.
- [x] **Layout & Styling**
  - [x] `MainLayout.razor`: Clean mobile-first app shell.
  - [x] iOS Safe Area Insets: CSS handling for notch and home bar (`padding-top: env(safe-area-inset-top)`).
  - [x] Theme Support: Automatic light/dark mode based on `prefers-color-scheme` with manual override toggle.
- [x] **Page Implementations**
  - [x] **`ReaderPage.razor` (Core Bible Reader)**:
    - [x] Renders current chapter with book title and chapter number.
    - [x] Displays verses with verse numbers, selectable for highlights/bookmarks.
    - [x] Displays inline pericope section headings.
    - [x] Footnote and cross-reference marks render inline; footnotes are also listed under a "Footnotes" heading in the verse action sheet.
    - [x] Swipe left/right touch gesture support for previous/next chapter navigation.
    - [x] Quick navigation header (tap book/chapter to open picker).
  - [x] **`LibraryPage.razor` (Version Management)**:
    - [x] Lists all installed Bible versions.
    - [x] Shows active version indicator.
    - [x] Allows switching active version with reader navigation.
    - [x] Version metadata view and delete button.
  - [x] **`ImportPage.razor` (File Uploader)**:
    - [x] File picker accepts `.yes` and `.yes.gz` files.
    - [x] Pre-parse validation display (Title, Language, Book Count).
    - [x] Import progress bar with streaming status.
  - [x] **`SearchPage.razor` (Full-Text Search)**:
    - [x] Search input with debounced execution.
    - [x] Scope filter (All, Old Testament, New Testament, Current Book).
    - [x] Query keyword highlighting and client-side pagination ("Show more", 50 per page).
    - [x] Tap result jumps directly to reader at that verse reference.
- [x] **Reusable Presentational Components**
  - [x] `ChapterView.razor`: Renders chapter verses and pericope headings; reader supports smooth verse scrolling.
  - [x] `VerseDisplay.razor`: Formatted verse text, highlight color background, footnote markers.
  - [x] `BookSelector.razor`: Modal / bottom drawer to pick Book (OT/NT grid) and Chapter number.
  - [x] `VerseActionSheet.razor`: Bottom drawer on verse tap (Highlight colors, Bookmark, Copy text).
  - [x] `SearchResultList.razor`: Search results renderer with ARI jump link.
  - [x] `LoadingSpinner.razor` and `ErrorDisplay.razor`.

---

## Phase 5: Polish, Release & Performance

- [x] **Reading Position Auto-Resume**
  - [x] Persist current reading position on chapter navigation.
  - [x] Automatically load last-read passage on app startup.
- [x] **Memory & Performance Optimization**
  - [x] Enable Blazor WASM IL trimming and Brotli compression in Release mode (explicit in `OpenBible.Wasm.csproj`; the Application assembly is rooted so MediatR handlers and validators survive trimming).
  - [ ] Profile memory during large `.yes` file imports (ensure streaming without memory spikes).
- [x] **Error Boundaries & Resilience**
  - [x] Add Blazor `<ErrorBoundary>` in layout for graceful crash recovery.
- [x] **CI Workflow**
  - [x] `.github/workflows/ci.yml` runs build/test, compose validation, and amd64/arm64 container smoke tests.
- [/] **Documentation Polish**
  - [x] README documents architecture, features, build/test/run, and current roadmap.
  - [ ] Add product screenshots and complete user-facing setup/usage guidance.
- [x] **Data Management**
  - [x] Clear reading history/bookmarks/highlights and provide a confirmed factory reset from the Library.
- [ ] **End-to-End Offline Verification**
  - [ ] Install PWA on iOS Safari / Android Chrome.
  - [ ] Import `.yes` file -> Navigate chapters -> Highlight verse -> Search -> Disconnect network -> Reload and verify complete offline functionality.

---

## Implemented features beyond the original milestones and remaining scope

- [x] Per-verse version comparison and side-by-side chapter panes (`/compare/{book}/{chapter}` with two translation selectors).
- [x] Notes, labels, progress marks, bundled sample reading plans and daily devotions, display settings, share, and go-to-reference are implemented (`/markers`, `/plans`, `/devotion`, `/compare`, `/settings`).
- [ ] Downloadable reading plans and devotions; only bundled samples are available.
- [x] Version management: Library lists versions, sets a default (`openbible.settings`), links to Import, and browses translations from the deployment-configured source registry. Install downloads through the `OpenBible.Server` proxy (`/api/sources/{source}/download/{translation}`), then imports locally. An empty registry leaves manual import available.
- [x] Backup/export and restore: More > Backup & restore transfers versioned JSON (positions, bookmarks, highlights, notes, labels, progress marks, plan progress, and default version), with rollback on invalid data (`IBackupService`, `UserDataBackupService`). Installed Bible texts and display settings are excluded.
- [ ] Sync of user data across devices.
- [ ] Songs.

---

## Phase 6: Multi-source, multi-format content (next milestone)

adr> Goal: ship the app **empty**. No embedded translation text, no embedded translation list, and no embedded hosts. The source registry (one host per format) comes from deployment configuration. Design is in [ADR 0004](docs/adr/0004-multi-source-bible-formats.md) (status: Proposed).
>
> Several items touch **ASK FIRST** areas in `AGENTS.md` (storage schema/contracts, `.yes` parsing structure, ARI). Get approval before implementing those.

- [x] **Decision & Design**
  - [x] Review and accept ADR 0004.
  - [x] Decide the versification strategy for non-66-book / differently numbered sources (ARI mapping: normalize to ARI, skip and report unmapped content).
  - [x] Decide which hosts to document in `sources.example.json` (placeholders only; none are active by default).
- [x] **Remove embedded content**
  - [x] Remove the bundled KJV and `BundledBibleSeeder`.
  - [x] Remove the embedded `version_config.json` and `BundledVersionCatalog`.
  - [x] Add an onboarding empty state (no translation installed) with links to the source browser and manual import.
  - [x] Keep installed data for existing users untouched.
- [x] **Source registry (deployment configuration)**
  - [x] Add `IBibleSource`, `ISourceCatalog`, and `ISourceRegistry` contracts (Domain).
  - [x] Server loads the registry from ASP.NET Core configuration (environment variables or a mounted JSON file); no default registry in the image.
  - [x] Expose the registry to the client through a small endpoint (`GET /api/sources`); an empty registry leaves manual import.
  - [x] Support catalog kinds `live` (query the host) and `file` (operator-supplied catalog, for example the former Yuku `version_config.json`).
  - [x] Add `sources.example.json` (documentation only, not loaded by default) and document it in the README.
  - [x] Derive the `OpenBible.Server` download allow-list from the registry; the hard-coded host list and legacy endpoint are removed.
- [x] **Proxy security**
  - [x] HTTPS only, and the host must match a registry entry; redirects are disabled rather than followed.
  - [x] Reject private, loopback, and link-local addresses (SSRF), including after DNS resolution.
  - [x] Enforce maximum download size, timeout, and allowed content types; forward no cookies or credentials.
  - [x] Offline tests for each rule (no live HTTP).
- [x] **Format abstraction**
  - [x] Add `IBibleFormatReader` and a reader selector by declared format plus file-signature detection for manual import.
  - [x] Move the `.yes` parser behind `IBibleFormatReader` with no behavior change; existing tests stay green.
  - [x] Store `Format` and `SourceId` with version metadata (snapshot-compatible).
- [x] **YES source** (existing format)
  - [x] Implement a runtime `ISourceCatalog` for the YES host; no embedded list.
- [x] **USFM source**
  - [x] USFM reader with offline fixtures and tests.
  - [x] Runtime catalog for the USFM host.
- [x] **SWORD source**
  - [x] SWORD module reader (zText/zText4, ZIP compression, `.conf`) with offline fixtures and tests.
  - [x] Versification mapping to ARI for KJV; other versifications and BZIP2/LZSS/XZ compression are rejected with a clear error.
  - [x] Runtime catalog for the SWORD host.
- [x] **Rights information**
  - [x] Show license text per translation before install when the source provides it; say so when it does not.
  - [x] Require confirmation before install and store the shown license text with the installed version.
- [x] **Documentation**
  - [x] Update README (empty-by-default behavior, sources, formats, rights notice) and remove references to the bundled KJV and the 251-preset catalog.
  - [x] Update Docker/CI smoke tests that assume a bundled KJV.
- [ ] **Verification**
  - [x] `dotnet build --no-incremental` and `dotnet test` pass with zero warnings; all tests offline.
  - [ ] Manual check: fresh install is empty, adding a translation from each source works, and offline reading works after install.
