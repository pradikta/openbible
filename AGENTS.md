# AGENTS.md — AI Agent Development Guide for OpenBible

> **Note**: This file serves as the Single Source of Truth for AI coding agents working on OpenBible.
> OpenBible is an offline-first .NET 10 Blazor WebAssembly (WASM) PWA porting the open-source [androidbible (Alkitab)](https://github.com/yukuku/androidbible) by Yuku to modern web standards.
> Every code generation, refactoring, or review action **must** strictly adhere to SOLID and Clean/Onion Architecture principles.

---

## 1. Three-Tier Operational Guardrails

### ALWAYS
1. **Verification Discipline**: Run a fresh `dotnet build` and `dotnet test` before declaring any task complete. Never trust cached terminal outputs.
2. **Offline-First Reality**: Design all components, storage, search, and data access assuming zero network connectivity. Target: iOS Safari standalone PWA.
3. **MediatR Orchestration**: All use cases, business logic, and UI commands/queries MUST flow through MediatR. Blazor components inject `IMediator` only.
4. **DTO Boundary**: Handlers return DTOs or `Result<T>`. Domain entities MUST NEVER leak into Blazor `.razor` components.
5. **Async & Cancellation**: Every I/O method must be `async Task`, append `Async`, accept `CancellationToken ct = default` as its final parameter, and use `ConfigureAwait(false)` in non-UI layers.
6. **Code Hygiene**: Clean up unused `using` directives (`IDE0005`) on all touched files. Enforce exactly **one type per file**.
7. **Conventional Commits**: Commit messages MUST follow Conventional Commits format (`feat(scope):`, `fix(scope):`, `test(scope):`, `chore:`). Valid scopes: `domain`, `app`, `infra`, `wasm`, `ci`, `docs`. Do not use subjective superlative adjectives.

### ASK FIRST
1. Adding any external NuGet package not in the approved package matrix.
2. Switching or replacing the client-side storage engine (IndexedDB vs SQLite WASM).
3. Modifying `.yes` binary parsing structures or ARI packing logic.
4. Altering the database schema or storage abstraction contracts.

### NEVER
1. **No Layer Inversion**: Never allow `Domain` to reference `Application`, `Infrastructure`, or `Wasm`. Never allow `Application` to reference `Infrastructure` or `Wasm`.
2. **No Direct Infra in UI**: Never inject repositories, parsers, or DbContexts into Blazor `.razor` files.
3. **No Sync-Over-Async**: Never call `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`, or `async void` (use `async Task`).
4. **No Full-File In-Memory Loading**: Never read an entire `.yes` file into a byte array. Stream section-by-section.
5. **No Live HTTP in Tests**: 100% of unit and integration tests must run offline with zero network calls.

---

## 2. Porting Guide: androidbible (Alkitab) → OpenBible .NET 10

When porting logic from [yukuku/androidbible](https://github.com/yukuku/androidbible), apply these strict idiom mappings:

### Architectural Concept Mapping
| Android (Java / Kotlin) | OpenBible (.NET 10 / Blazor WASM) | Notes / SOLID Principle |
|---|---|---|
| `Activity` / `Fragment` | Blazor Page (`Pages/ReaderPage.razor`) | Single Responsibility: UI rendering only |
| Custom View / `Adapter` | Blazor Component (`Components/ChapterView.razor`) | Presentational with `[Parameter]` & `EventCallback` |
| `ViewModel` / `LiveData` | MediatR Query + DTO (`GetChapterQuery` -> `ChapterDto`) | Clean separation of UI state from business logic |
| `IntArrayList` / Java arrays | `IReadOnlyList<T>`, `ReadOnlyMemory<byte>`, `[.. collection]` | Modern C# 12/13 memory efficiency |
| `getFoo()` / `setFoo()` | C# Auto-Properties (`public int Verse { get; init; }`) | Idiomatic C# encapsulation |
| `ARI` (Alkitab Resource Index) | `VerseReference` Value Object (`Domain/ValueObjects/`) | Encapsulate packing bit shifts inside the VO |
| `YesReader` / `Yes2Reader` | `YesFileParser` implementing `IYesFileReader` (`Infrastructure`) | Strategy / Open-Closed: modular section readers |
| `ContentProvider` / SQLite | `IBibleRepository` -> IndexedDB-backed repository (memory fallback) | Dependency Inversion: abstract storage behind interface |
| `SharedPreferences` | `IUserDataRepository` -> LocalStorage / IndexedDB | Abstract reading position, history, bookmarks |
| Java `DataInputStream` (Big-Endian) | `BinaryReader` + `BinaryPrimitives.ReadInt32BigEndian` | Java binary numbers are always Big-Endian |

### ARI (Alkitab Resource Identifier) Specification
The androidbible project packs book, chapter, and verse into a compact 32-bit integer (**ARI**):
- **Formula**: `ari = (bookId << 16) | (chapter << 8) | verse`
- **Book ID**: 0-indexed integer (`Genesis = 0` through `Revelation = 65`).
- **Chapter**: 1-indexed integer.
- **Verse**: 1-indexed integer (`0` designates the entire chapter).
- **Example**: `0x000100` = decimal `256` = Genesis Chapter 1 (entire chapter).
- **Domain VO Rule**: Port this strictly into `VerseReference` Value Object with `ToAri()` and `static FromAri(int ari)`. Never write bit-shift math in Application or UI layers.

### `.yes` (YES2) Binary Format Rules
1. **Signature**: 8-byte header starting with `0x98, 0x58` magic bytes.
2. **Transparent Decompression**: If file ends with `.yes.gz` or starts with gzip magic `0x1F, 0x8B`, wrap stream in `System.IO.Compression.GZipStream`.
3. **Section Architecture**: Headers point to named sections (`versionInfo`, `booksInfo`, `text`, `pericopes`, `xrefs`, `footnotes`).
4. **Open-Closed Section Readers**: Implement `IYesSectionReader` for each section type so new sections can be added without modifying the core parser.

---

## 3. Onion Architecture & Dependencies

```
┌────────────────────────────────────────────────────────┐
│               Presentation: OpenBible.Wasm              │
│  ┌──────────────────────────────────────────────────┐  │
│  │       Infrastructure: OpenBible.Infrastructure   │  │
│  │  ┌────────────────────────────────────────────┐  │  │
│  │  │       Application: OpenBible.Application   │  │  │
│  │  │  ┌──────────────────────────────────────┐  │  │  │
│  │  │  │       Domain: OpenBible.Domain       │  │  │  │
│  │  │  └──────────────────────────────────────┘  │  │  │
│  │  └────────────────────────────────────────────┘  │  │
│  └──────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────┘
```

`OpenBible.Server` is a separate host that composes Application and Infrastructure for the source registry, catalog cache, download proxy, and admin page.

- **`OpenBible.Domain`**: Pure C#. Contains Entities, Value Objects, Enums, Domain Exceptions, and Interface Contracts (`IBibleRepository`, `IYesFileReader`, `IBibleFormatReader`, `ISearchIndex`, `IUserDataRepository`, `IBibleSource`, `ISourceCatalog`, `ISourceRegistry`, `ISourceDirectory`). It references `MediatR.Contracts` for planned `INotification` domain events; none are published yet. **Zero other dependencies**.
- **`OpenBible.Application`**: Use cases orchestrated via MediatR Commands, Queries, Handlers, Pipeline Behaviors (`LoggingBehavior`, `ValidationBehavior`), and DTOs. Depends on `Domain` only.
- **`OpenBible.Infrastructure`**: `.yes` binary parsing, Snappy/GZip decompression, USFM/SWORD readers, source catalog adapters, download validation, and IndexedDB-backed repositories with an in-memory fallback. Depends on `Domain` and `Application`.
- **`OpenBible.Wasm`**: Blazor WebAssembly PWA host, UI pages, razor components, service worker, CSS safe-area layout. Depends on `Application` (via `IMediator`) and registers `Infrastructure` only in `Program.cs`.
- **`OpenBible.Server`**: ASP.NET Core host, source registry, live catalog cache, SSRF-protected download proxy, and token-protected `/admin` page/API. Depends on `Application` and `Infrastructure`.

---

## 4. Code Standards & Anti-Pattern Examples

### 4.1 MediatR Query & Handler (CORRECT vs INCORRECT)

```csharp
// ❌ WRONG: Leaking Domain Entity to UI, mixing I/O with business logic, no CancellationToken
public class BadHandler : IRequestHandler<GetChapterQuery, Chapter> // NEVER return Domain Entity
{
    private readonly ApplicationDbContext _db; // NEVER depend directly on DbContext in handler
    public BadHandler(ApplicationDbContext db) => _db = db;
    public async Task<Chapter> Handle(GetChapterQuery req, CancellationToken ct)
        => _db.Chapters.First(c => c.Number == req.Chapter); // NEVER use blocking LINQ First()
}

// ✅ CORRECT: Clean Architecture, returns DTO, async streaming, cancellation, ConfigureAwait
public sealed class GetChapterHandler(IBibleRepository bibleRepository) 
    : IRequestHandler<GetChapterQuery, Result<ChapterDto>>
{
    public async Task<Result<ChapterDto>> Handle(GetChapterQuery request, CancellationToken ct)
    {
        var verses = await bibleRepository
            .GetChapterVersesAsync(request.VersionId, request.BookId, request.Chapter, ct)
            .ConfigureAwait(false);

        if (verses.Count == 0)
            return Result<ChapterDto>.Failure("Chapter not found.");

        var dtos = verses.Select(v => new VerseDto(v.Reference.Verse, v.Text, v.Reference.ToAri())).ToList();
        return Result<ChapterDto>.Success(new ChapterDto(request.BookId, request.BookId.ToString(), request.Chapter, dtos, []));
    }
}
```

### 4.2 Blazor Razor Component (CORRECT vs INCORRECT)

```razor
@* ❌ WRONG: Direct repository access and business logic in UI *@
@page "/read"
@inject IBibleRepository Repo  @* VIOLATION: Never inject repositories *@
@code {
    protected override async Task OnInitializedAsync() {
        var verses = await Repo.GetVersesAsync("kjv", BookId.Genesis, 1); // VIOLATION
    }
}

@* ✅ CORRECT: Thin UI component orchestrating via IMediator and DTO *@
@page "/read/{VersionId}/{Book:int}/{Chapter:int}"
@inject IMediator Mediator

<PageTitle>@_title</PageTitle>

@if (_result is { IsSuccess: true, Value: not null })
{
    <ChapterView Chapter="_result.Value" />
}
else if (_loading)
{
    <LoadingSpinner />
}
else
{
    <ErrorDisplay Message="@_result?.Error" />
}

@code {
    [Parameter] public string VersionId { get; set; } = default!;
    [Parameter] public int Book { get; set; }
    [Parameter] public int Chapter { get; set; }

    private Result<ChapterDto>? _result;
    private bool _loading = true;
    private string _title = "Reading";

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _result = await Mediator.Send(new GetChapterQuery(VersionId, (BookId)Book, Chapter));
        _title = _result.IsSuccess ? $"{_result.Value!.BookName} {Chapter}" : "Error";
        _loading = false;
    }
}
```

---

## 5. Build, Test & Solution Commands

- **Target**: .NET 10 (`net10.0`), C# 12/13.
- **Solution File**: `OpenBible.sln` (classic `.sln` format, created with `dotnet new sln -n OpenBible --format sln`).
- **Central Package Management**: Enabled via `Directory.Packages.props`. Never declare `<Version>` inside `.csproj` files.
- **Shared Properties**: Configured in `Directory.Build.props` (`Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`).

```bash
# Restore packages centrally
dotnet restore

# Build with zero warnings and zero errors
dotnet build --no-incremental

# Run all unit and integration tests offline
dotnet test --no-build --verbosity normal

# Run the app locally (Wasm host plus catalog API)
dotnet run --project src/OpenBible.Server
```

---

## 6. Execution Milestones

Live progress lives in [PLAN.md](PLAN.md), which is the single source of truth for completed and pending work. The intended build order is:

0. Scaffolding and solution baseline.
1. Domain core and ARI addressing.
2. Application CQRS use cases.
3. Infrastructure (`.yes` parser and offline storage).
4. Blazor WASM PWA presentation.
5. Polish and release.
6. Multi-source, multi-format content (proposed in [ADR 0004](docs/adr/0004-multi-source-bible-formats.md)).

Each milestone must pass `dotnet build` and `dotnet test` with zero warnings and zero errors before the next one starts.
