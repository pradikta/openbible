# OpenBible

> **Modern, offline-first .NET 10 Blazor WebAssembly PWA port of [androidbible (Alkitab)](https://github.com/yukuku/androidbible)**

OpenBible brings the proven architecture and binary `.yes` format of the open-source *Alkitab* Android application by Yuku to modern browsers. Designed as an offline-first Progressive Web App (PWA) tailored for standalone mobile use (iOS Safari & Android Chrome), OpenBible delivers instant, zero-latency scripture reading and searching without requiring a server connection.

---

## 🌟 Key Features

- **Offline-First PWA**: Operates 100% offline via Service Worker caching and local client-side persistence (IndexedDB / SQLite WASM).
- **Native `.yes` & `.yes.gz` Parser**: Direct, streaming support for the Alkitab YES2 binary translation format with Snappy decompression.
- **ARI Addressing Engine**: Full compatibility with the 32-bit Alkitab Resource Identifier (`(book << 16) | (chapter << 8) | verse`).
- **Smooth Reading Experience**: Touch swipe navigation between chapters, pericope section headings, parallel verses, and footnotes.
- **Full-Text Scripture Search**: Local inverted index search with instant matching across installed translations.
- **User Annotations**: Bookmarks, color-coded highlights, notes, and automatic reading position tracking.
- **iOS Standalone Optimized**: Safe-area insets (`env(safe-area-inset-*)`), clean touch interactions, and system dynamic dark/light mode.

---

## 🏗️ Architecture

OpenBible follows strict **Onion Architecture** and **CQRS via MediatR**:

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

| Project | Description | Dependencies |
|---|---|---|
| `OpenBible.Domain` | Entities, Value Objects (`VerseReference`, ARI), interfaces (`IBibleRepository`, `IYesFileReader`), Domain Events. | None (`MediatR.Contracts` only) |
| `OpenBible.Application` | CQRS use cases (Commands, Queries, Handlers, Behaviors, DTOs). | `OpenBible.Domain` |
| `OpenBible.Infrastructure` | Binary `.yes` parser, Snappy/GZip streams, IndexedDB/SQLite WASM persistence. | `Domain`, `Application` |
| `OpenBible.Wasm` | Blazor WebAssembly PWA host, UI components, Service Worker, iOS layout. | `Application` (via MediatR), `Infrastructure` (DI only) |

---

## 🚀 Tech Stack

- **Framework**: .NET 10 (`net10.0`)
- **Frontend**: Blazor WebAssembly, HTML5, CSS3 (iOS Safe Area support)
- **Mediator**: MediatR CQRS
- **Validation**: FluentValidation
- **Storage**: IndexedDB (`DnetIndexedDb` or WebAssembly SQLite)
- **Decompression**: Managed Snappy + BCL `GZipStream`
- **Testing**: xUnit, NSubstitute, FluentAssertions (100% offline tests)
- **Package Management**: Central Package Management (`Directory.Packages.props`)

---

## 🛠️ Build & Verification

```bash
# Restore dependencies centrally
dotnet restore

# Build solution with zero warnings and zero errors
dotnet build --no-incremental

# Run all automated tests offline
dotnet test --no-build --verbosity normal

# Run local development server
dotnet run --project src/OpenBible.Wasm
```

---

## 📋 Project Roadmap & Status

See [PLAN.md](file:///e:/source/github/openbible/PLAN.md) for the active milestone checklist and [AGENTS.md](file:///e:/source/github/openbible/AGENTS.md) for architectural rules and development guidelines.

---

## 📄 License

Open-source under the [MIT License](LICENSE). Based on logic and specifications from [androidbible (Alkitab)](https://github.com/yukuku/androidbible) by Yuku.

