# Phase 2 — Syntax Parsing Infrastructure

## Status

- ✅ Phase 1 (regex engine) complete with 521 passing tests.
- ⏳ Phase 1 Cleanup pending — see `PHASE1_CLEANUP.md`. Must complete before Phase 2 implementation begins.
- ⏳ Phase 2 (this document) not started.

## Scope

Port the C++ syntax parsing infrastructure to .NET (`Far.Colorer`). Must produce region output identical to the C++ parser on real source files using real HRC/HRD definitions.

Out of scope for Phase 2: NuGet packaging, CI pipeline, async/cancellation tokens, asynchronous parsing, IDE integration. These are handled at the wider project level later.

## Architectural Decisions

The following decisions were made during the planning review and apply throughout Phase 2:

| Decision | Rationale |
|---|---|
| `ColorerRegex` stays `internal` | .NET's `System.Text.RegularExpressions` covers editor-side regex search; Colorer's regex engine is HRC/parser-only |
| Sync API only — no `async` / `Task` / `CancellationToken` | C++ `breakParse()` maps to a synchronous flag; editor integration is sync |
| Per-instance regex match serialization is acceptable | One parse job at a time per editor; no shared-instance parallelism |
| XML loading: DOM via `System.Xml.Linq.XDocument` / `XElement` | Mirrors C++ libxml2 + `XMLNode` (children list, attribute map, recursive descent); SAX would require state-machine rewrite |
| HRC loading: 3-phase lazy (`PROTOTYPE` / `TYPE` / `FULL`) | Catalogs reference ~50 HRC files; eager load is wasteful at startup |
| `FileTypeChooser` is in scope | Required to map filename + first line → `FileType` |
| VTList / virtual entries are in scope (not deferred) | Required for cpp inheriting from c, java, etc. — i.e., real-world parsing |
| HRD support is its own group | Parser output is unusable without color-scheme mapping |
| Region output API is both push and pull | Push: `IRegionHandler` callbacks (matches C++). Pull: built-in handler that stores results, exposed via `GetLineRegions(line)` |
| `IDisposable` cascade is acceptable | All callers belong to the same project; transitively-disposable ownership chain (`ParserFactory` → `HrcLibrary` → `FileType` → `Scheme` → `SchemeNode` → `ColorerRegex`) is documented and managed |
| No NuGet, no CI in this phase | Handled at the project level later |

## Architecture Overview

```
Catalog (catalog.xml)
    │
    ├── HRC files ─── HrcLibrary ──── FileType ──── Scheme ──── SchemeNode
    │                                                              │
    ├── HRD files ─── HrdLibrary ──── RegionMapper                  │
    │                                                              │
    └── ParserFactory ─── TextParser ──────────────────────────────┘
                            │           │
                            ↓           ↓
                       LineSource   RegionHandler (push) / Stored regions (pull)
```

Dependency direction: TextParser depends on everything else. HrcLibrary and HrdLibrary depend on the XML and IO layers. Region is a leaf type.

## Public API Shape

The Phase 2 public surface (to project-internal consumers):

```csharp
namespace Far.Colorer;

public interface IParserFactory : IDisposable {
    void LoadCatalog(string catalogPath);
    IHrcLibrary HrcLibrary { get; }
    IHrdLibrary HrdLibrary { get; }
    IFileType ChooseFileType(string fileName, string firstLine, int typeNo = 0);
    ITextParser CreateTextParser();
    IRegionMapper CreateStyledMapper(string hrdName);
    IRegionMapper CreateTextMapper(string hrdName);
}

public interface ITextParser : IDisposable {
    void SetFileType(IFileType type);
    void SetLineSource(ILineSource source);
    void SetRegionHandler(IRegionHandler handler);  // push (optional)
    int Parse(int from, int count, TextParseMode mode);
    void BreakParse();   // synchronous flag, not Task cancellation
    IReadOnlyList<LineRegion> GetLineRegions(int line);  // pull (always available)
}

public interface IRegionHandler {
    void StartParsing(int from, int count);
    void EndParsing();
    void AddRegion(int line, int start, int end, IRegion region);
    void EnterScheme(int line, int start, int end, IRegion region);
    void LeaveScheme(int line, int start, int end, IRegion region);
}

public interface ILineSource {
    int LineCount { get; }
    string GetLine(int lineNumber);
    void StartJob(int from, int count);
    void EndJob(int from, int count);
}
```

Both push (`IRegionHandler`) and pull (`GetLineRegions`) are always supported. The parser internally accumulates regions via a built-in default handler if no explicit handler is set; `GetLineRegions` reads from that built-in handler. If a consumer passes their own `IRegionHandler`, both their handler and the built-in store receive events.

## Implementation Groups

Groups are ordered by dependency. Within a group, sub-tasks may be parallelized; across groups, later ones depend on earlier ones being functional (not necessarily complete with full polish).

---

### Group 0: Golden-Data Harness (Prerequisite)

Build the parity test infrastructure *before* writing TextParser, not after.

**Tasks.**
- Extend the C++ `colorer` CLI (or write a small new C++ binary using the existing library) to dump regions as JSON. Output schema: `{ line, start, end, region_name, event: "add"|"enter"|"leave" }` per record.
- Add a small .NET harness in `Far.Colorer.Tests/Integration/` that: (1) shells out to the C++ binary on a test source file to produce reference JSON, (2) runs the .NET parser on the same file, (3) emits .NET regions as the same JSON schema, (4) diffs them.
- Pre-generate reference JSON for at least 5 test files: a C file, a C++ file, a Java file, a CMake file, an XML file. Store under `Far.Colorer.Tests/Data/golden/`.
- Document in this file how to regenerate golden data when HRC files change (e.g., a script in `tools/regenerate_golden.sh`).

**Definition of Done.**
- C++ binary produces JSON region output for any `(file, FileType)` pair.
- .NET test harness diffs C++ vs .NET output and reports first divergence with line/position.
- Golden JSON exists for 5+ files and is stored in version control.
- Regenerate-golden script runs on Windows and Linux.

---

### Group 1: Foundation Types

**Files (C++ → C#).**
- `Region.h` → `Far.Colorer.Regions.Region` + `IRegion` interface
- `Common.h` / `Exception.h` → expand `Far.Colorer.RegularExpressions.ColorerException` or move to `Far.Colorer.ColorerException`

**Notes.**
- Region IDs are sequential; assign with `Interlocked.Increment` against a static counter for thread safety during HRC load.
- Region parent chain (`hasParent` recursion) must match C++ semantics exactly.
- Immutable after construction.

**Definition of Done.**
- `Region` with parent chain, ID, name, description.
- `RegionTests.cs` with parent-traversal scenarios (10+ cases including no-parent, self-parent rejection, multi-level chains).
- A concurrent-creation test confirms ID uniqueness under contention.

---

### Group 2: Keyword System

**Files.**
- `parsers/KeywordList.h/cpp` → `Far.Colorer.Parsers.KeywordList`

**Notes.**
- Sorted list with binary search.
- `minKeywordLength` and first-character optimizations are critical.
- Symbol vs word keyword distinction (symbols don't require word boundaries).
- Use `ReadOnlySpan<char>` for matching to avoid allocation.

**Definition of Done.**
- Binary search produces the same matches as C++ on the keyword list of a real HRC file (`c.hrc` or `cpp.hrc`).
- Case-sensitive and case-insensitive modes covered.
- Symbol keyword matching covered.
- Sort comparison matches C++ behavior (verify with a unit test using a representative keyword set).

---

### Group 3: Scheme Nodes & Scheme

**Files.**
- `parsers/SchemeNode.h` → abstract `SchemeNode` + `SchemeNodeRegexp`, `SchemeNodeBlock`, `SchemeNodeKeywords`, `SchemeNodeInherit`
- `parsers/VirtualEntry.h` → `VirtualEntry` (virtual entries used by inherit nodes)
- `Scheme.h` + `parsers/SchemeImpl.h/cpp` → `IScheme` + `Scheme`

**Notes.**
- Region arrays sized to match `MATCHES_NUM` from the regex engine.
- `lowPriority`, `lowContentPriority`, `innerRegion` flags affect TextParser match logic — preserve exactly.
- Nodes stored in declaration order (execution order matters).
- VTList virtual-entry resolution is part of this group, not deferred.

**Definition of Done.**
- All four node types implemented and unit-tested independently.
- Region assignment to numbered + named captures verified.
- Virtual entry resolution covered with at least one inheritance scenario (e.g., a test scheme overrides an inherited node).

---

### Group 4: FileType + FileTypeChooser

**Files.**
- `FileType.h` + `parsers/FileTypeImpl.h/cpp` → `IFileType` + `FileType`
- `parsers/FileTypeChooser.h/cpp` → `FileTypeChooser` (filename + first-line → `FileType`)

**Notes.**
- Parameter system: `<param name="..." value="..." />` with default vs user-set values, `getParamValueInt`/`Hex` helpers.
- Lazy `GetBaseScheme()` triggers the type body to load (PROTOTYPE → FULL transition).
- `FileTypeChooser` evaluates filename regex + first-line regex from the prototype to pick a type.

**Definition of Done.**
- Parameter get/set with defaults works.
- `FileTypeChooser.Choose("foo.cpp", "/* line 1 */")` returns the cpp type.
- Lazy load is observable: type body is not parsed until `GetBaseScheme` is first called.

---

### Group 5: XML & I/O Infrastructure

**Files.**
- `xml/XMLNode.h` (DOM) → use `System.Xml.Linq.XDocument` + `XElement` directly (no wrapper type — LINQ to XML is the wrapper).
- `xml/XmlInputSource.h` → `Far.Colorer.IO.IInputSource` (`Stream GetInputStream()`).
- Implementations: `FileInputSource`, `StreamInputSource`, `ZipInputSource` (uses `System.IO.Compression.ZipArchive`).

**Notes.**
- DTD entity resolution is required for HRC includes (`&c-unix;`, `&regexp;`). Configure `XmlReaderSettings.DtdProcessing = DtdProcessing.Parse` when constructing the `XmlReader` that feeds `XDocument.Load`.
- `XDocument` API is what the HRC/HRD parsers use directly. Recursive descent matches C++ structure.
- `IInputSource` covers files, streams, ZIP entries, and embedded resources via a uniform `Stream` interface.

**Definition of Done.**
- `IInputSource` implementations work for file, stream, and ZIP.
- A small test loads `data/base/hrc/base/base.hrc` (which uses entities) and verifies entity expansion (e.g., that an attribute containing `&c-unix;` resolves to the entity body).

---

### Group 6: HRC Library (Lazy 3-Phase Load)

**Files.**
- `HrcLibrary.h` + `parsers/HrcLibraryImpl.h/cpp` → `IHrcLibrary` + `HrcLibrary`
- `parsers/CatalogParser.h/cpp` → `CatalogParser`

**Three load phases.**
- `PROTOTYPE` — at startup, parse only `<prototype>` blocks across all referenced HRC files. Builds the `FileType` registry without loading scheme bodies. Cheap.
- `TYPE` — parse `<type>` metadata (parameters, region declarations) for one type. Triggered on `GetFileType(name)`.
- `FULL` — parse `<scheme>` bodies for one type. Triggered on `FileType.GetBaseScheme()`.

**Notes.**
- Region ID assignment is thread-safe (`Interlocked`). Document that HRC loading itself is single-threaded; only ID generation needs to be lock-free.
- Scheme name resolution uses `type:scheme` separator.
- `<if>` / `<unless>` attribute evaluation: small DSL evaluating against the FileType's parameter values. Implement as a recursive-descent evaluator over `XElement` attributes.
- VTList integration: when a `SchemeNodeInherit` resolves, apply virtual entries to override matching nodes in the inherited scheme.

**Definition of Done.**
- All ~50 HRC files in `data/base/hrc/base/` load without errors using PROTOTYPE phase.
- `GetFileType("c")` triggers TYPE-phase load; `GetBaseScheme()` triggers FULL.
- Entity resolution and `<if>`/`<unless>` evaluation work on real HRC files.
- `cpp.hrc` correctly inherits from `c.hrc`; a sanity test parses a tiny C++ snippet and the inherited C-keyword regions appear.

---

### Group 7: HRD Library + Region Mapper

**Files.**
- HRD parsing logic (no separate `.h` to point at — read `data/base/hrd/`'s structure and the `RegionMapper` family in `handlers/`)
- `handlers/RegionMapper.h/cpp` → `IRegionMapper`
- `handlers/StyledHRDMapper.h/cpp` → `StyledHRDMapper` (foreground/background colors + styles)
- `handlers/TextHRDMapper.h/cpp` → `TextHRDMapper` (console output: ANSI/escape sequences)
- `handlers/StyledRegion.h/cpp` → `StyledRegion`
- `handlers/TextRegion.h/cpp` → `TextRegion`
- `handlers/RegionDefine.h` → `RegionDefine`

**Notes.**
- HRD files are XML mapping region names → visual styles (`<assign name="def:String" fore="#F"/>`).
- HRD inherits across files (a console HRD typically inherits from a base HRD).
- Mapper provides O(1) region-ID-indexed lookup.
- Two flavors: styled (RGB + bold/italic/underline) and text (ANSI escape sequences for terminal output).

**Definition of Done.**
- Both `StyledHRDMapper` and `TextHRDMapper` implemented.
- All HRD files in `data/base/hrd/` load without errors.
- A test maps a parsed region (e.g., `def:String`) through both mappers and verifies the expected style/escape sequence.

---

### Group 8: Line Source & Region Handler

**Files.**
- `LineSource.h` → `ILineSource` + `StringLineSource` (in-memory string array).
- `RegionHandler.h` (interface in handlers) → `IRegionHandler`.
- `handlers/LineRegion.h/cpp` → `LineRegion`.
- `handlers/LineRegionsSupport.h/cpp` → `LineRegionsSupport` (built-in pull-mode handler — stores regions per line).
- `handlers/LineRegionsCompactSupport.h/cpp` → `LineRegionsCompactSupport` (memory-efficient variant).

**Notes.**
- `IRegionHandler` is the push-mode interface; the parser calls `AddRegion` / `EnterScheme` / `LeaveScheme`.
- `LineRegionsSupport` is the default pull-mode storage. The parser always feeds it (so `GetLineRegions(line)` works even when no external handler is set), and *also* feeds any caller-provided `IRegionHandler` if one was set via `SetRegionHandler`.

**Definition of Done.**
- `ILineSource`, `IRegionHandler`, `LineRegion`, `LineRegionsSupport` all implemented.
- A test confirms that with no caller-provided handler, `GetLineRegions(0)` returns the right regions after a parse.
- A test confirms that a caller-provided handler receives the same events as the internal store.

---

### Group 9: Text Parser

**Files.**
- `TextParser.h` + `parsers/TextParserImpl.h/cpp` → `ITextParser` + `TextParser`
- `parsers/TextParserHelpers.h/cpp` → cache structures, VTList management

**Critical correctness requirements.**
- All four search methods: `searchKW`, `searchRE`, `searchBL`, `searchIN`.
- Match priority exactly matches C++: higher nodes match first unless `lowPriority` is set; `lowContentPriority` affects content-vs-end matching inside blocks.
- Cross-pattern backreferences in block end regex (`\y2`, `\y{name}`) reference start-regex captures.
- Stack-based scheme nesting with `MAX_RECURSION_LEVEL` (100) enforced.
- Three parse modes: `CACHE_OFF`, `CACHE_READ`, `CACHE_UPDATE`.
- `BreakParse()` sets a flag that the parsing loop checks between scheme-node iterations.

**Cache implementation.**
- C++ uses raw pointer linked lists. .NET options: managed class hierarchy with `ObjectPool<T>` for cache nodes, OR a struct-based arena. Start with the managed class + pool approach; profile and switch only if GC pressure exceeds the target (<1MB Gen0/1000 lines).

**Definition of Done.**
- All search methods implemented.
- Parse a single line (`int x = 5;`) produces correct keyword + identifier + number regions.
- Parse `data/tests/test/cpp/testcases.c` matches C++ output 100% via the golden-data harness.
- All three cache modes work: a sequence of `CACHE_UPDATE` then `CACHE_READ` parse calls produces consistent regions.
- All 5+ golden-data files pass exact match.
- Stack-overflow protection verified with a deep-nesting test.

---

### Group 10: Parser Factory & Integration

**Files.**
- `ParserFactory.h` + `parsers/ParserFactoryImpl.h/cpp` → `IParserFactory` + `ParserFactory`

**Tasks.**
- Builder/factory API per the public API shape above.
- Loads catalog from a directory or a ZIP file.
- Wires up `HrcLibrary`, `HrdLibrary`, and creates `TextParser` instances.

**Definition of Done.**
- End-to-end test: `var factory = new ParserFactory(); factory.LoadCatalog("data/base/catalog.xml"); var fileType = factory.ChooseFileType("test.cpp", "// hello"); var parser = factory.CreateTextParser(); parser.SetFileType(fileType); parser.Parse(...)` works on a real source file.
- All disposability semantics verified — disposing the factory disposes everything transitively.

---

## Incremental Integration Milestones

Vertical slices to validate end-to-end behavior before the full TextParser lands.

**M1 — Hello Region.** Create a `Region` programmatically, verify ID and parent chain.

**M2 — Golden-data harness operational.** Group 0 complete; can produce reference JSON for at least one test file from C++ side.

**M3 — Parse a Keyword.** Minimal in-memory HRC with one keyword loads and matches.

**M4 — Load Real Catalog.** Full `data/base/catalog.xml` loads via PROTOTYPE phase; all ~50 HRC files load without errors. `cpp.hrc` correctly inherits from `c.hrc` (verify via VTList resolution).

**M5 — Parse One Line.** TextParser with no cache parses `int x = 5;` correctly. Both push handler and `GetLineRegions(0)` return identical results.

**M6 — Parse Real File.** Full TextParser with `CACHE_UPDATE` parses `testcases.c` with 100% region match vs C++ via the golden-data harness.

**M7 — HRD Mapping.** Parsed regions from M6 mapped through `StyledHRDMapper` and `TextHRDMapper` produce expected styles / ANSI sequences.

**M8 — Full Integration.** End-to-end via `ParserFactory` on at least 5 different language files (C, C++, Java, CMake, XML). All match C++ output 100%.

## Testing Strategy

**Unit tests** — each class in isolation. Mirror C++ unit tests where they exist (`native/tests/unit/`), expand where coverage gaps exist.

**Integration tests** — load real HRC/HRD files from `native/data/`, parse small snippets.

**Golden-data tests** — the harness from Group 0; the authoritative parity check.

**Test data** — the build copies `native/data/base/` and `native/data/tests/test/` into the test project's output. Don't duplicate in source control; copy at build time.

## Non-Functional Checklist

To be verified per PR for code in the relevant area:

**Correctness**
- [ ] Behavior matches C++ for any parser-visible decision (priority, backreferences, recursion limit, cache invalidation).
- [ ] Region IDs are stable within a single library load.
- [ ] Golden data passes exactly (no near-misses accepted).

**Performance** (targets, not gates — we don't have C++ baselines yet)
- [ ] Parse rate informally observed; track regressions across PRs.
- [ ] No Gen2 collections during steady-state parsing.
- [ ] Use `Span<char>` / `ReadOnlySpan<char>` on hot paths.

**Code quality**
- [ ] No new compiler warnings.
- [ ] Nullable reference types annotated correctly.
- [ ] `unsafe` only in regex internals (already isolated to `Far.Colorer.RegularExpressions.Internal`).
- [ ] XML docs on public types and methods.

**Disposability**
- [ ] Every type that transitively owns a `ColorerRegex` is `IDisposable`.
- [ ] `Dispose` is idempotent.
- [ ] Test that disposing a parent (`HrcLibrary`, `ParserFactory`) disposes its children.

**Cross-platform**
- [ ] `Path.Combine` for path construction; never hardcode `\` or `/`.
- [ ] ZIP entry paths handled correctly (forward slashes inside ZIP, regardless of host OS).

## Exit Criteria

Phase 2 is complete when:
1. All groups above are at "Definition of Done."
2. All 8 milestones pass.
3. Golden-data parity holds on at least 5 files (C, C++, Java, CMake, XML).
4. All HRC files in `data/base/hrc/base/` load without errors.
5. All HRD files in `data/base/hrd/` load without errors.
6. The 2 known-skipped regex tests remain skipped (out of scope).
