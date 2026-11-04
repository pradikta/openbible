# OpenBible — Master Execution Plan & Feature Checklist

> **Single Source of Truth for Project Progress**  
> Update this checklist incrementally as tasks are started `[/]` and completed `[x]`.  
> Every milestone must pass `dotnet build` and `dotnet test` with zero warnings and zero errors before proceeding.
>
> **Progress snapshot (2026-11-04):** Phases 0–2 are substantially implemented; Phase 3 is in progress
> (the core parser exists, but section readers are placeholders and persistent browser storage/Snappy
> decompression are not implemented); Phase 4 has its main pages and components, while Phase 5 remains
> partial. Latest verification: `dotnet build --no-incremental` succeeded with 0 warnings/errors and
> `dotnet test --no-build --verbosity normal` passed all 44 tests. Browser/device offline behavior has
> not been verified end-to-end.

---

## 🧭 Milestone Overview

```
Phase 0: Scaffolding ──► Phase 1: Domain Core ──► Phase 2: Application CQRS
                                                           │
Phase 5: Release ◄── Phase 4: Blazor UI & PWA ◄── Phase 3: Infra & .yes Parser
```

---

## Phase 0: Solution Scaffolding & CI Baseline

- [x] **SDK & Tooling Setup**
  - [x] Create `global.json` pinning .NET 10 SDK version (`10.0.100` or latest installed).
  - [x] Create classic solution file `OpenBible.sln` via `dotnet new sln -n OpenBible --format sln`.
  - [x] Create `.gitignore` tailored for .NET 10, Blazor WASM, and IDEs.
- [/] **Centralized MSBuild Configurations**
  - [x] Create `Directory.Build.props`:
    - [x] `TargetFramework`: `net10.0`
    - [x] `Nullable`: `enable`
    - [x] `ImplicitUsings`: `enable`
    - [x] `TreatWarningsAsErrors`: `true`
  - [/] Create `Directory.Packages.props` for Central Package Management (CPM):
    - [x] Pin `MediatR` (and `MediatR.Contracts`)
    - [x] Pin `FluentValidation`
    - [x] Pin `Microsoft.AspNetCore.Components.WebAssembly`
    - [x] Pin `xunit`, `NSubstitute`, `FluentAssertions`
    - [ ] Pin Snappy decompression library (managed WASM compatible)
    - [ ] Pin IndexedDB interop package
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
  - [x] `Verse`: `VerseReference Reference`, `string Text`, `IReadOnlyList<FootnoteEntry> Footnotes`.
  - [x] `Chapter`: `BookId Book`, `int ChapterNumber`, `IReadOnlyList<Verse> Verses`.
  - [x] `Book`: `BookId BookId`, `string ShortName`, `string FullName`, `int ChapterCount`, `IReadOnlyList<int> VerseCounts`.
  - [x] `BibleVersion`: `string Id`, `string Title`, `string Language`, `string Description`, `IReadOnlyList<Book> Books`.
  - [x] `Pericope`: Section heading with `VerseReference Reference`, `string Title`, `IReadOnlyList<string> ParallelPassages`.
  - [x] `FootnoteEntry`: `VerseReference Reference`, `int Index`, `string Content`.
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
- [/] **Unit Tests (`OpenBible.Domain.Tests`)**
  - [x] ARI packing/unpacking roundtrip tests for boundary values (Gen 1:1, Rev 22:21, whole chapter 0x000100).
  - [x] `VerseReference` equality, comparison, and invalid input exceptions.
  - [x] `VerseRange` containment tests.
  - [ ] Entity invariants and guard clauses.

---

## Phase 2: Application CQRS & Business Logic (Backend)

- [x] **Common Core & Monads**
  - [x] Implement `Result<T>` and `Result` in `Common/Models/` (Success, Failure, Error reasons).
  - [x] Pipeline Behaviors:
    - [x] `LoggingBehavior<TRequest, TResponse>` (intercepts requests, measures execution time).
    - [x] `ValidationBehavior<TRequest, TResponse>` (validates via FluentValidation, returns `Result.Failure`).
- [x] **Data Transfer Objects (DTOs)**
  - [x] `ChapterDto(BookId BookId, string BookName, int ChapterNumber, IReadOnlyList<VerseDto> Verses, IReadOnlyList<PericopeDto> Pericopes)`
  - [x] `VerseDto(int VerseNumber, string Text, int Ari, bool IsBookmarked, HighlightColor? Highlight)`
  - [x] `PericopeDto(int BeforeVerse, string Title, IReadOnlyList<string> Parallels)`
  - [x] `BibleVersionDto(string Id, string Title, string Language, string Description, int TotalBooks)`
  - [x] `SearchResultDto(VerseReference Reference, string VersionId, string BookName, int Chapter, int Verse, string Snippet, double Score)`
  - [x] `BookmarkDto`, `HighlightDto`, `ReadingPositionDto`
- [/] **Use Cases: Bible Reading & Navigation**
  - [x] `GetChapterQuery` + `GetChapterHandler` + `GetChapterValidator`
  - [ ] `GetVerseQuery` + `GetVerseHandler`
  - [/] `GetBookListQuery` + `GetBookListHandler` (book catalog and chapter counts exist; version-specific book data is not implemented)
- [/] **Use Cases: Version Management & Import**
  - [x] `GetInstalledVersionsQuery` + `GetInstalledVersionsHandler`
  - [ ] `GetVersionMetadataQuery` + `GetVersionMetadataHandler`
  - [/] `PreviewYesFileQuery` + handler provides import metadata preview; a separate installed-version metadata query is not implemented.
  - [/] `ImportYesFileCommand` + `ImportYesFileHandler` (handler exists; parser and persistent-storage gaps remain)
  - [x] `DeleteVersionCommand` + `DeleteVersionHandler`
- [x] **Use Cases: Search**
  - [x] `SearchBibleQuery` + `SearchBibleHandler` (supports full-text queries, scope filtering by Testament or Book)
- [x] **Use Cases: User Data**
  - [x] `GetReadingPositionQuery` + `GetReadingPositionHandler`
  - [x] `SaveReadingPositionCommand` + `SaveReadingPositionHandler`
  - [x] `ToggleBookmarkCommand` + `ToggleBookmarkHandler`
  - [x] `SetVerseHighlightCommand` + `SetVerseHighlightHandler`
  - [x] `GetBookmarksQuery` + `GetBookmarksHandler`
- [x] **Dependency Injection Registration**
  - [x] Create `DependencyInjection.cs` with `AddApplication(this IServiceCollection)` registering MediatR, FluentValidation, and Behaviors.
- [/] **Unit Tests (`OpenBible.Application.Tests`)**
  - [x] Isolated handler tests using `NSubstitute` to mock `IBibleRepository` and `IYesFileReader`.
  - [ ] Validation behavior tests (invalid query inputs return failure).

---

## Phase 3: Infrastructure Layer (YES Parser & Offline Storage)

- [/] **`.yes` Binary Format Architecture**
- [x] Document specification in `docs/adr/0003-yes-file-format-spec.md` (layouts verified against upstream readers; local parser gaps recorded).
  - [/] `YesFileConstants`: Header magic bytes `0x98 0x58` are present; section names still need to match upstream (`versionInfo`, `booksInfo`, `text`, `pericopes`, `xrefs`, `footnotes`).
  - [x] Implement `BigEndianBinaryReader` wrapping `Stream` with `BinaryPrimitives.Read*BigEndian`.
- [/] **Section Readers (`Infrastructure/YesFile/SectionReaders/`)**
  - [x] `IYesSectionReader` contract (Open-Closed design).
  - [ ] `MetadataSectionReader`: parses version title, language, book ordering, text encodings.
  - [ ] `BookNamesSectionReader`: parses full and short book titles.
  - [ ] `VerseTextSectionReader`: streams compressed verse text blocks, unpacks chapter offsets.
  - [ ] `PericopeSectionReader`: extracts pericope headings and parallel passage cross-references.
- [/] **Decompression Engine**
  - [/] Transparent `.yes.gz` GZip stream wrapper (`System.IO.Compression.GZipStream` exists; seek-dependent parsing still needs support).
  - [ ] Managed WASM-compatible Snappy block decompressor for YES2 compressed verse text.
- [/] **Core Parser**
  - [/] Implement `YesFileParser` implementing `IYesFileReader`:
    - [x] Validates the YES magic bytes (`0x98 0x58`) in the 8-byte header.
    - [x] Reads section index table without loading whole file into memory.
    - [/] Asynchronously yields verses and pericopes section-by-section (iterator exists; format/decompression and section-reader work remain).
- [ ] **Offline Storage Implementations**
  - [ ] `IndexedDbBibleRepository` implementing `IBibleRepository` using client-side IndexedDB interop.
  - [ ] `IndexedDbUserDataRepository` implementing `IUserDataRepository` for bookmarks, highlights, and history.
  - [ ] `IndexedDbSearchIndex` implementing `ISearchIndex` with tokenized text search.
- [/] **Dependency Injection Registration**
  - [/] Create `DependencyInjection.cs` with `AddInfrastructure(this IServiceCollection)` registering parser and in-memory repositories/index (browser-persistent storage interop remains unimplemented).
- [/] **Integration Tests (`OpenBible.Infrastructure.Tests`)**
  - [ ] Create minimal fixture `.yes` test file in `tests/fixtures/sample.yes`.
  - [/] Test `YesFileParser` header validation and section parsing (header validation is covered; full fixture section parsing remains).
  - [ ] Test Snappy decompression accuracy against known sample blocks.
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
- [/] **Page Implementations**
  - [/] **`ReaderPage.razor` (Core Bible Reader)**:
    - [x] Renders current chapter with book title and chapter number.
    - [x] Displays verses with verse numbers, selectable for highlights/bookmarks.
    - [x] Displays inline pericope section headings.
    - [ ] Displays footnotes accessible via tap (UI supports footnotes, but the YES parser does not populate them yet).
    - [x] Swipe left/right touch gesture support for previous/next chapter navigation.
    - [x] Quick navigation header (tap book/chapter to open picker).
  - [x] **`LibraryPage.razor` (Version Management)**:
    - [x] Lists all installed Bible versions.
    - [x] Shows active version indicator.
    - [x] Allows switching active version with reader navigation.
    - [x] Version metadata view and delete button.
  - [/] **`ImportPage.razor` (File Uploader)**:
    - [x] File picker accepts `.yes` and `.yes.gz` files.
    - [x] Pre-parse validation display (Title, Language, Book Count).
    - [ ] Import progress bar with streaming status.
  - [/] **`SearchPage.razor` (Full-Text Search)**:
    - [x] Search input with debounced execution.
    - [x] Scope filter (All, Old Testament, New Testament, Current Book).
    - [/] Query keyword highlighting is implemented; pagination or virtualization remains.
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
- [ ] **Memory & Performance Optimization**
  - [ ] Enable Blazor WASM IL trimming and Brotli compression in Release mode.
  - [ ] Profile memory during large `.yes` file imports (ensure streaming without memory spikes).
- [x] **Error Boundaries & Resilience**
  - [x] Add Blazor `<ErrorBoundary>` in layout for graceful crash recovery.
- [ ] **CI Workflow & Documentation**
  - [ ] Create `.github/workflows/ci.yml` (Runs `dotnet build` + `dotnet test` on PRs and pushes).
  - [ ] Complete `README.md` with screenshots and usage guides.
- [ ] **End-to-End Offline Verification**
  - [ ] Install PWA on iOS Safari / Android Chrome.
  - [ ] Import `.yes` file -> Navigate chapters -> Highlight verse -> Search -> Disconnect network -> Reload and verify complete offline functionality.
