# OpenBible — Master Execution Plan & Feature Checklist

> **Single Source of Truth for Project Progress**  
> Update this checklist incrementally as tasks are started `[/]` and completed `[x]`.  
> Every milestone must pass `dotnet build` and `dotnet test` with zero warnings and zero errors before proceeding.

---

## 🧭 Milestone Overview

```
Phase 0: Scaffolding ──► Phase 1: Domain Core ──► Phase 2: Application CQRS
                                                           │
Phase 5: Release ◄── Phase 4: Blazor UI & PWA ◄── Phase 3: Infra & .yes Parser
```

---

## Phase 0: Solution Scaffolding & CI Baseline

- [ ] **SDK & Tooling Setup**
  - [ ] Create `global.json` pinning .NET 10 SDK version (`10.0.100` or latest installed).
  - [ ] Create classic solution file `OpenBible.sln` via `dotnet new sln -n OpenBible --format sln`.
  - [ ] Create `.gitignore` tailored for .NET 10, Blazor WASM, and IDEs.
- [ ] **Centralized MSBuild Configurations**
  - [ ] Create `Directory.Build.props`:
    - [ ] `TargetFramework`: `net10.0`
    - [ ] `Nullable`: `enable`
    - [ ] `ImplicitUsings`: `enable`
    - [ ] `TreatWarningsAsErrors`: `true`
  - [ ] Create `Directory.Packages.props` for Central Package Management (CPM):
    - [ ] Pin `MediatR` (and `MediatR.Contracts`)
    - [ ] Pin `FluentValidation`
    - [ ] Pin `Microsoft.AspNetCore.Components.WebAssembly`
    - [ ] Pin `xunit`, `NSubstitute`, `FluentAssertions`
    - [ ] Pin Snappy decompression library (managed WASM compatible)
    - [ ] Pin IndexedDB interop package
- [ ] **Scaffold Source Projects (`src/`)**
  - [ ] `src/OpenBible.Domain` (classlib, zero external dependencies)
  - [ ] `src/OpenBible.Application` (classlib, references `Domain`)
  - [ ] `src/OpenBible.Infrastructure` (classlib, references `Domain` + `Application`)
  - [ ] `src/OpenBible.Wasm` (blazorwasm project, references `Application` + `Infrastructure`)
- [ ] **Scaffold Test Projects (`tests/`)**
  - [ ] `tests/OpenBible.Domain.Tests` (xunit, references `Domain`)
  - [ ] `tests/OpenBible.Application.Tests` (xunit, references `Application` + `Domain`)
  - [ ] `tests/OpenBible.Infrastructure.Tests` (xunit, references `Infrastructure` + `Domain` + `Application`)
- [ ] **Verification Gate**
  - [ ] Run `dotnet restore`
  - [ ] Run `dotnet build --no-incremental` (must succeed with 0 warnings, 0 errors)
  - [ ] Run `dotnet test --no-build`

---

## Phase 1: Domain Core & ARI Addressing Engine (Backend)

- [ ] **Enums & Identifiers**
  - [ ] Define `BookId` enum (`0` to `65`, matching androidbible zero-based index: Genesis = 0, Revelation = 65).
  - [ ] Define `Testament` enum (`OldTestament`, `NewTestament`).
  - [ ] Define `YesSectionType` enum (`VersionInfo`, `BookNames`, `VerseText`, `Pericope`, `Xref`, `Footnotes`).
  - [ ] Define `HighlightColor` enum (`Yellow`, `Green`, `Blue`, `Pink`, `Purple`).
- [ ] **Value Objects (Immutable & Structurally Equatable)**
  - [ ] Implement `VerseReference`:
    - [ ] `BookId Book`, `int Chapter` (1-based), `int Verse` (1-based, 0 = whole chapter).
    - [ ] `int ToAri()` bit-shift packing: `(bookId << 16) | (chapter << 8) | verse`.
    - [ ] `static VerseReference FromAri(int ari)` unpacking.
    - [ ] `string ToDisplayString(string? bookName = null)`.
    - [ ] Structural equality (`IEquatable<VerseReference>`) and operator overloads (`==`, `!=`).
  - [ ] Implement `VerseRange`:
    - [ ] `VerseReference Start`, `VerseReference End`.
    - [ ] Range validation (same book/chapter, `Start.Verse <= End.Verse`).
    - [ ] `bool Contains(VerseReference reference)`.
  - [ ] Implement `YesFileDescriptor`:
    - [ ] File metadata, format version (`YES1` vs `YES2`), byte length, is compressed (`.gz`).
- [ ] **Domain Entities**
  - [ ] `Verse`: `VerseReference Reference`, `string Text`, `IReadOnlyList<FootnoteEntry> Footnotes`.
  - [ ] `Chapter`: `BookId Book`, `int ChapterNumber`, `IReadOnlyList<Verse> Verses`.
  - [ ] `Book`: `BookId BookId`, `string ShortName`, `string FullName`, `int ChapterCount`, `IReadOnlyList<int> VerseCounts`.
  - [ ] `BibleVersion`: `string Id`, `string Title`, `string Language`, `string Description`, `IReadOnlyList<Book> Books`.
  - [ ] `Pericope`: Section heading with `VerseReference Reference`, `string Title`, `IReadOnlyList<string> ParallelPassages`.
  - [ ] `FootnoteEntry`: `VerseReference Reference`, `int Index`, `string Content`.
  - [ ] `Bookmark`: `Guid Id`, `string VersionId`, `VerseReference Reference`, `DateTimeOffset CreatedAt`, `string? Note`.
  - [ ] `Highlight`: `Guid Id`, `string VersionId`, `VerseReference Reference`, `HighlightColor Color`, `DateTimeOffset UpdatedAt`.
  - [ ] `ReadingPosition`: `string VersionId`, `VerseReference Reference`, `DateTimeOffset LastReadAt`.
- [ ] **Repository & Service Contracts (Interfaces)**
  - [ ] `IBibleRepository`:
    - [ ] `Task<IReadOnlyList<BibleVersion>> GetInstalledVersionsAsync(CancellationToken ct = default)`
    - [ ] `Task<BibleVersion?> GetVersionAsync(string versionId, CancellationToken ct = default)`
    - [ ] `Task<IReadOnlyList<Verse>> GetChapterVersesAsync(string versionId, BookId book, int chapter, CancellationToken ct = default)`
    - [ ] `Task<IReadOnlyList<Pericope>> GetChapterPericopesAsync(string versionId, BookId book, int chapter, CancellationToken ct = default)`
    - [ ] `Task SaveVersionAsync(BibleVersion version, IAsyncEnumerable<Verse> verses, CancellationToken ct = default)`
    - [ ] `Task DeleteVersionAsync(string versionId, CancellationToken ct = default)`
  - [ ] `IYesFileReader`:
    - [ ] `Task<BibleVersion> ReadMetadataAsync(Stream stream, CancellationToken ct = default)`
    - [ ] `IAsyncEnumerable<Verse> ReadVersesAsync(Stream stream, CancellationToken ct = default)`
    - [ ] `IAsyncEnumerable<Pericope> ReadPericopesAsync(Stream stream, CancellationToken ct = default)`
  - [ ] `ISearchIndex`:
    - [ ] `Task IndexVersionAsync(string versionId, IAsyncEnumerable<Verse> verses, CancellationToken ct = default)`
    - [ ] `Task<IReadOnlyList<SearchResult>> SearchAsync(SearchQuery query, CancellationToken ct = default)`
  - [ ] `IUserDataRepository`:
    - [ ] Reading position save & retrieve
    - [ ] Bookmarks CRUD
    - [ ] Highlights CRUD
- [ ] **Domain Exceptions & Events**
  - [ ] `YesFileCorruptException`, `VerseNotFoundException`, `VersionAlreadyExistsException`.
  - [ ] `BibleVersionImportedEvent`, `ReadingPositionChangedEvent`.
- [ ] **Unit Tests (`OpenBible.Domain.Tests`)**
  - [ ] ARI packing/unpacking roundtrip tests for boundary values (Gen 1:1, Rev 22:21, whole chapter 0x000100).
  - [ ] `VerseReference` equality, comparison, and invalid input exceptions.
  - [ ] `VerseRange` containment tests.
  - [ ] Entity invariants and guard clauses.

---

## Phase 2: Application CQRS & Business Logic (Backend)

- [ ] **Common Core & Monads**
  - [ ] Implement `Result<T>` and `Result` in `Common/Models/` (Success, Failure, Error reasons).
  - [ ] Pipeline Behaviors:
    - [ ] `LoggingBehavior<TRequest, TResponse>` (intercepts requests, measures execution time).
    - [ ] `ValidationBehavior<TRequest, TResponse>` (validates via FluentValidation, returns `Result.Failure`).
- [ ] **Data Transfer Objects (DTOs)**
  - [ ] `ChapterDto(BookId BookId, string BookName, int ChapterNumber, IReadOnlyList<VerseDto> Verses, IReadOnlyList<PericopeDto> Pericopes)`
  - [ ] `VerseDto(int VerseNumber, string Text, int Ari, bool IsBookmarked, HighlightColor? Highlight)`
  - [ ] `PericopeDto(int BeforeVerse, string Title, IReadOnlyList<string> Parallels)`
  - [ ] `BibleVersionDto(string Id, string Title, string Language, string Description, int TotalBooks)`
  - [ ] `SearchResultDto(VerseReference Reference, string VersionId, string BookName, int Chapter, int Verse, string Snippet, double Score)`
  - [ ] `BookmarkDto`, `HighlightDto`, `ReadingPositionDto`
- [ ] **Use Cases: Bible Reading & Navigation**
  - [ ] `GetChapterQuery` + `GetChapterHandler` + `GetChapterValidator`
  - [ ] `GetVerseQuery` + `GetVerseHandler`
  - [ ] `GetBookListQuery` + `GetBookListHandler` (returns all books and their chapter counts for a version)
- [ ] **Use Cases: Version Management & Import**
  - [ ] `GetInstalledVersionsQuery` + `GetInstalledVersionsHandler`
  - [ ] `GetVersionMetadataQuery` + `GetVersionMetadataHandler`
  - [ ] `ImportYesFileCommand` + `ImportYesFileHandler` (streams `.yes` via `IYesFileReader`, persists via `IBibleRepository`, triggers search indexing)
  - [ ] `DeleteVersionCommand` + `DeleteVersionHandler`
- [ ] **Use Cases: Search**
  - [ ] `SearchBibleQuery` + `SearchBibleHandler` (supports full-text queries, scope filtering by Testament or Book)
- [ ] **Use Cases: User Data**
  - [ ] `GetReadingPositionQuery` + `GetReadingPositionHandler`
  - [ ] `SaveReadingPositionCommand` + `SaveReadingPositionHandler`
  - [ ] `ToggleBookmarkCommand` + `ToggleBookmarkHandler`
  - [ ] `SetVerseHighlightCommand` + `SetVerseHighlightHandler`
  - [ ] `GetBookmarksQuery` + `GetBookmarksHandler`
- [ ] **Dependency Injection Registration**
  - [ ] Create `DependencyInjection.cs` with `AddApplication(this IServiceCollection)` registering MediatR, FluentValidation, and Behaviors.
- [ ] **Unit Tests (`OpenBible.Application.Tests`)**
  - [ ] Isolated handler tests using `NSubstitute` to mock `IBibleRepository` and `IYesFileReader`.
  - [ ] Validation behavior tests (invalid query inputs return failure).

---

## Phase 3: Infrastructure Layer (YES Parser & Offline Storage)

- [ ] **`.yes` Binary Format Architecture**
  - [ ] Document specification in `docs/adr/0003-yes-file-format-spec.md`.
  - [ ] `YesFileConstants`: Header magic bytes `0x98 0x58`, section names (`version_info`, `book_names`, `verse_text`, `pericope`, `xref`).
  - [ ] Implement `BigEndianBinaryReader` wrapping `Stream` with `BinaryPrimitives.Read*BigEndian`.
- [ ] **Section Readers (`Infrastructure/YesFile/SectionReaders/`)**
  - [ ] `IYesSectionReader` contract (Open-Closed design).
  - [ ] `MetadataSectionReader`: parses version title, language, book ordering, text encodings.
  - [ ] `BookNamesSectionReader`: parses full and short book titles.
  - [ ] `VerseTextSectionReader`: streams compressed verse text blocks, unpacks chapter offsets.
  - [ ] `PericopeSectionReader`: extracts pericope headings and parallel passage cross-references.
- [ ] **Decompression Engine**
  - [ ] Transparent `.yes.gz` GZip stream wrapper (`System.IO.Compression.GZipStream`).
  - [ ] Managed WASM-compatible Snappy block decompressor for YES2 compressed verse text.
- [ ] **Core Parser**
  - [ ] Implement `YesFileParser` implementing `IYesFileReader`:
    - [ ] Validates 8-byte magic header (`0x98 0x58`).
    - [ ] Reads section index table without loading whole file into memory.
    - [ ] Asynchronously yields verses and pericopes section-by-section.
- [ ] **Offline Storage Implementations**
  - [ ] `IndexedDbBibleRepository` implementing `IBibleRepository` using client-side IndexedDB interop.
  - [ ] `IndexedDbUserDataRepository` implementing `IUserDataRepository` for bookmarks, highlights, and history.
  - [ ] `IndexedDbSearchIndex` implementing `ISearchIndex` with tokenized text search.
- [ ] **Dependency Injection Registration**
  - [ ] Create `DependencyInjection.cs` with `AddInfrastructure(this IServiceCollection)` registering parser, repositories, and storage interop.
- [ ] **Integration Tests (`OpenBible.Infrastructure.Tests`)**
  - [ ] Create minimal fixture `.yes` test file in `tests/fixtures/sample.yes`.
  - [ ] Test `YesFileParser` header validation and section parsing against fixture.
  - [ ] Test Snappy decompression accuracy against known sample blocks.
  - [ ] Test Big-Endian integer reading logic.

---

## Phase 4: Presentation Layer (Blazor WASM iOS PWA UI)

- [ ] **Composition Root & PWA Assets**
  - [ ] `Program.cs`: Register `.AddApplication().AddInfrastructure()`.
  - [ ] `manifest.webmanifest`: PWA metadata (`display: standalone`, icons, theme colors).
  - [ ] `index.html`: Apple mobile tags (`apple-mobile-web-app-capable`, `apple-mobile-web-app-status-bar-style`, viewport-fit=cover).
  - [ ] `service-worker.js`: Offline-first caching for WASM assemblies, CSS, and app assets.
- [ ] **Layout & Styling**
  - [ ] `MainLayout.razor`: Clean mobile-first app shell.
  - [ ] iOS Safe Area Insets: CSS handling for notch and home bar (`padding-top: env(safe-area-inset-top)`).
  - [ ] Theme Support: Automatic light/dark mode based on `prefers-color-scheme` with manual override toggle.
- [ ] **Page Implementations**
  - [ ] **`ReaderPage.razor` (Core Bible Reader)**:
    - [ ] Renders current chapter with book title and chapter number.
    - [ ] Displays verses with verse numbers, selectable for highlights/bookmarks.
    - [ ] Displays inline pericope section headings.
    - [ ] Displays footnotes accessible via tap.
    - [ ] Swipe left/right touch gesture support for previous/next chapter navigation.
    - [ ] Quick navigation header (tap book/chapter to open picker).
  - [ ] **`LibraryPage.razor` (Version Management)**:
    - [ ] Lists all installed Bible versions.
    - [ ] Shows active version indicator.
    - [ ] Allows switching active version with instant reader refresh.
    - [ ] Version metadata view and delete button.
  - [ ] **`ImportPage.razor` (File Uploader)**:
    - [ ] Drag-and-drop or file picker for `.yes` and `.yes.gz` files.
    - [ ] Pre-parse validation display (Title, Language, Book Count).
    - [ ] Import progress bar with streaming status.
  - [ ] **`SearchPage.razor` (Full-Text Search)**:
    - [ ] Search input with instant or debounced execution.
    - [ ] Scope filter (All, Old Testament, New Testament, Current Book).
    - [ ] Paginated or virtualized search result list with query keyword highlighting.
    - [ ] Tap result jumps directly to reader at that verse reference.
- [ ] **Reusable Presentational Components**
  - [ ] `ChapterView.razor`: Virtualized or smooth-scrolling verse list.
  - [ ] `VerseDisplay.razor`: Formatted verse text, highlight color background, footnote markers.
  - [ ] `BookSelector.razor`: Modal / bottom drawer to pick Book (OT/NT grid) and Chapter number.
  - [ ] `VerseActionSheet.razor`: Bottom drawer on verse tap (Highlight colors, Bookmark, Copy text).
  - [ ] `SearchResultList.razor`: Search results renderer with ARI jump link.
  - [ ] `LoadingSpinner.razor` and `ErrorDisplay.razor`.

---

## Phase 5: Polish, Release & Performance

- [ ] **Reading Position Auto-Resume**
  - [ ] Persist current reading position on chapter navigation.
  - [ ] Automatically load last-read passage on app startup.
- [ ] **Memory & Performance Optimization**
  - [ ] Enable Blazor WASM IL trimming and Brotli compression in Release mode.
  - [ ] Profile memory during large `.yes` file imports (ensure streaming without memory spikes).
- [ ] **Error Boundaries & Resilience**
  - [ ] Add Blazor `<ErrorBoundary>` in layout for graceful crash recovery.
- [ ] **CI Workflow & Documentation**
  - [ ] Create `.github/workflows/ci.yml` (Runs `dotnet build` + `dotnet test` on PRs and pushes).
  - [ ] Complete `README.md` with screenshots and usage guides.
- [ ] **End-to-End Offline Verification**
  - [ ] Install PWA on iOS Safari / Android Chrome.
  - [ ] Import `.yes` file -> Navigate chapters -> Highlight verse -> Search -> Disconnect network -> Reload and verify complete offline functionality.

