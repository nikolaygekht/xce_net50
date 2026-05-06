# Colorer-Library .NET Port - Project Overview

## Project Description

Colorer-library is a syntax highlighting library originally implemented in C++. This document provides key information for porting the library to .NET.

## Purpose

The library provides syntax highlighting capabilities for text editors and other applications by:
- Parsing text using customizable syntax definitions (HRC files)
- Applying color schemes to highlighted regions (HRD files)
- Supporting complex nested region matching with regular expressions

## Architecture Overview

### Core Components

#### 1. **HRC Files (Syntax Definitions)**
Located in: `data/base/hrc/`

HRC files are XML-based syntax definition files that define:
- **Regions**: Text patterns enclosed by start/end markers
- **Schemes**: Collections of parsing rules
- **Inheritance**: Schemes can inherit from other schemes
- **Regular Expressions**: Used extensively to define region boundaries

**Example Structure** (from `data/base/hrc/base/c.hrc`):
```xml
<scheme name="String">
  <block start="/(?{def:StringEdge}(L|U|u8?)?&#34;)/"
         end="/(?{def:StringEdge}&#34;)/"
         scheme="StringContent" region="String"/>
</scheme>
```

**Key Features**:
- XML-based with DTD validation
- Uses entities for reusable patterns
- Supports nested blocks and inheritance
- Extensive use of backreferences in regex patterns (e.g., `\y2` for back-referencing)

#### 2. **HRD Files (Color Schemes)**
Located in: `data/base/hrd/`

HRD files are XML-based color scheme definitions that map region names to visual styles.

**Example Structure** (from `data/base/hrd/console/black.hrd`):
```xml
<assign name="def:String" fore="#F"/>
<assign name="def:Number" fore="#6"/>
<assign name="def:Comment" fore="#8"/>
```

**Key Features**:
- Maps region names to foreground/background colors
- Supports console and styled output modes
- Region names reference those defined in HRC files

#### 3. **Regular Expression Engine (CRegExp)**
Located in: `native/src/colorer/cregexp/`

**Critical Component** - 1546 lines of custom regex implementation

**File**: `native/src/colorer/cregexp/cregexp.h`, `cregexp.cpp`

**Special Features**:
- Custom implementation for Colorer-specific extensions
- Unicode support (16-bit character units)
- Extended backreference support including:
  - `\yN` - backreference to another RE's bracket
  - `\YN` - negative backreference
  - `\y{name}`, `\Y{name}` - named backreferences across patterns
- Named capture groups: `(?{name} pattern)`
- Look-ahead/behind: `?=`, `?!`, `?#N`, `?~N`
- Colorer-specific metacharacters:
  - `~` - start of scheme
  - `\m`, `\M` - set new start/end of zero bracket
- Unicode character classes: `[{L}]`, `[{Nd}]`, `[{ALL}]`, etc.
- Character class operations: subtraction, intersection, union

**Operators** (from EOps enum):
```cpp
ReMul, RePlus, ReQuest,           // *, +, ?
ReNGMul, ReNGPlus, ReNGQuest,     // *?, +?, ?? (non-greedy)
ReRangeN, ReRangeNM,              // {n,}, {n,m}
ReBrackets, ReNamedBrackets,      // (...), (?{name} ...)
ReBkTrace, ReBkTraceName,         // \yN, \y{name}
```

**.NET Target**:
- **Namespace**: `Far.Colorer.RegularExpressions`
- **Main Classes**: `CRegExp`, `SMatches`, `Character`, `CharacterClass`
- **Status**: ✅ **COMPLETED in Phase 1**
- **Key Features Used**:
  - `unsafe` code for performance-critical pattern matching
  - `Span<char>` and `ReadOnlySpan<char>` for zero-copy string operations
  - Stackalloc for temporary buffers
  - Direct UTF-16 manipulation (compatible with .NET `string`)
- **Deviations**: None - exact port of C++ behavior including all edge cases

#### 4. **Parser Factory**
Located in: `native/src/colorer/ParserFactory.h`

**Responsibilities**:
- Loads `catalog.xml` to discover HRC and HRD files
- Creates and manages HrcLibrary instances
- Creates TextParser instances
- Creates RegionMapper instances (StyledHRDMapper, TextHRDMapper)

**.NET Target**:
- **Namespace**: `Far.Colorer`
- **Main Classes**: `IParserFactory`, `ParserFactoryImpl`
- **Dependencies**: System.IO.Compression (ZIP support), System.Xml.Linq
- **Key Features**:
  - Factory pattern with fluent builder API
  - Supports loading from directory, ZIP archive, or embedded resources
  - Thread-safe singleton for catalog loading
  - Lazy initialization of HRC/HRD libraries
- **Deviations**: Builder pattern more idiomatic in .NET than C++ factory methods

#### 5. **Text Parser**
Located in: `native/src/colorer/TextParser.h`, `parsers/TextParserImpl.h`

**Core Parsing Engine**:
- Implements cacheable syntax parsing
- Maintains parse tree for incremental updates
- Supports three parse modes:
  - `TPM_CACHE_OFF` - Full parse from root
  - `TPM_CACHE_READ` - Use cache for positioning
  - `TPM_CACHE_UPDATE` - Update cache during parse
- Uses `LineSource` interface for input
- Uses `RegionHandler` for output

**Key Methods**:
```cpp
void setFileType(FileType* type);
void setLineSource(LineSource* lh);
void setRegionHandler(RegionHandler* rh);
int parse(int from, int num, TextParseMode mode);
```

**.NET Target**:
- **Namespace**: `Far.Colorer.Parsers`
- **Main Classes**: `ITextParser`, `TextParserImpl`, `TextParseMode` enum
- **Key Features**:
  - `Span<char>` for line processing without allocations
  - Object pooling for parse cache structures to minimize GC pressure
  - `unsafe` code in hot path (scheme node matching)
  - Reusable match result buffers
- **Performance Targets**:
  - Parse rate: >10,000 lines/sec for typical code files
  - Memory overhead: <100 bytes per cached line
  - GC pressure: <1MB allocations per 1000 lines parsed
- **Critical**: Exact match priority and backtracking behavior must match C++

#### 6. **HRC Library**
Located in: `native/src/colorer/HrcLibrary.h`

**Responsibilities**:
- Manages all loaded HRC syntax definitions
- Provides access to FileType definitions
- Manages scheme inheritance and resolution

**.NET Target**:
- **Namespace**: `Far.Colorer.Parsers`
- **Main Classes**: `IHrcLibrary`, `HrcLibraryImpl`, `CatalogParser`
- **Dependencies**: System.Xml.Linq for HRC parsing
- **Key Features**:
  - Thread-safe region ID assignment using `Interlocked`
  - Immutable `FileType` and `Scheme` objects after loading
  - Dictionary-based lookups for schemes (`type:scheme` format)
  - XML entity resolution for HRC includes
- **Deviations**: LINQ to XML instead of libxml2, but maintains compatible entity handling

#### 7. **Region System**
Located in: `native/src/colorer/Region.h`, `handlers/`

**Purpose**:
- Defines region types (syntax elements)
- Maps regions to visual representations
- Handlers process region information for output

**.NET Target**:
- **Namespace**: `Far.Colorer.Regions` (core), `Far.Colorer.Handlers` (handlers)
- **Main Classes**: `Region`, `IRegionHandler`, `LineRegion`, `LineRegionsSupport`
- **Key Features**:
  - Immutable `Region` with structural equality (record class candidate)
  - Parent hierarchy using immutable linked list pattern
  - ID-based array indexing for O(1) region mapper lookup
- **Handlers**: `LineRegionsSupport`, `LineRegionsCompactSupport`, custom implementations
- **Deviations**: Use C# record for Region if beneficial; otherwise sealed class

#### 8. **String Handling**
Located in: `native/src/colorer/strings/`

**Two Implementations**:
- `icu/` - ICU library-based (modern, default)
- `legacy/` - Legacy implementation

**Key Classes**:
- `UnicodeString` - 16-bit Unicode string container
- `Character` - Unicode character utilities
- `CharacterClass` - Character classification

**.NET Target**:
- **Namespace**: `Far.Colorer.RegularExpressions` (Character/CharacterClass already ported)
- **Status**: ✅ **COMPLETED in Phase 1** (Character utilities)
- **Key Features**:
  - Use native .NET `string` (UTF-16, compatible with C++ 16-bit units)
  - `ReadOnlySpan<char>` for zero-copy substring operations
  - `Character.GetUnicodeCategory()` wraps `char.GetUnicodeCategory()`
  - `CharacterClass` for Unicode category sets
- **Deviations**: No separate UnicodeString wrapper - use `string` directly
- **Dependency**: None - .NET BCL provides all Unicode support needed

#### 9. **XML Processing**
Located in: `native/src/colorer/xml/`

**Implementation**:
- Uses libxml2 for parsing HRC/HRD files
- `XmlReader` interface for XML access
- `XmlInputSource` for file/stream input

**.NET Target**:
- **Namespace**: `Far.Colorer.Xml`
- **Main Classes**: `IXmlReader`, `SaxXmlReader` (wraps `XmlReader`)
- **Dependencies**: System.Xml (XmlReader - forward-only SAX-style parser)
- **Key Features**:
  - `XmlReader` for streaming, forward-only parsing (SAX pattern)
  - Low memory footprint - doesn't build DOM tree
  - Entity resolution via `XmlReaderSettings.DtdProcessing`
  - Attribute access via reader position
- **Entity Resolution**: `XmlUrlResolver` for HRC entity references (`&c-unix;`, `&regexp;`)
- **Performance**: Streaming parser avoids allocating full document tree
- **Deviations**: .NET `XmlReader` is pull-based SAX; C++ libxml2 is push-based SAX, but parsing logic identical

#### 10. **I/O System**
Located in: `native/src/colorer/io/`

**Components**:
- `InputSource` - Abstract input source
- `FileInputSource` - File-based input
- ZIP support for compressed HRC/HRD catalogs

**.NET Target**:
- **Namespace**: `Far.Colorer.IO`
- **Main Classes**: `IInputSource`, `FileInputSource`, `ZipInputSource`, `StreamInputSource`
- **Dependencies**: System.IO.Compression.ZipArchive
- **Key Features**:
  - Abstract `IInputSource` with `Stream GetInputStream()` method
  - `FileInputSource` wraps `FileStream`
  - `ZipInputSource` wraps `ZipArchive` entries
  - Supports embedded resources via `Assembly.GetManifestResourceStream()`
- **Deviations**: .NET Stream abstraction cleaner than C++ custom I/O; all sources provide Stream interface

## Dependencies

### C++ Version Dependencies
- **ICU** (International Components for Unicode) - string handling
- **libxml2** - XML parsing
- **minizip** - ZIP archive support
- **zlib** - compression

### Build System
- CMake 3.10+
- C++14 or higher (gcc 7+, clang 7+, MSVC 2019+)

---

### .NET Version Dependencies and Mappings

#### Runtime Requirements
- **.NET Target**: .NET 6.0 or higher (LTS)
  - Rationale: `Span<T>`, unsafe code improvements, good performance
  - C# Language Version: 10.0 or higher

#### NuGet Packages Required

| C++ Dependency | .NET Equivalent | NuGet Package | Purpose | Migration Notes |
|----------------|-----------------|---------------|---------|-----------------|
| **ICU** | .NET BCL | *(none)* | Unicode support | .NET `string` is UTF-16 native; `char.GetUnicodeCategory()` provides character classification; no external package needed |
| **libxml2** | System.Xml | *(built-in)* | XML parsing | Use `XmlReader` (SAX-style streaming parser) from System.Xml; DTD entity resolution via `XmlReaderSettings` |
| **minizip** | System.IO.Compression | *(built-in)* | ZIP support | `ZipArchive` class provides read/write access to ZIP files |
| **zlib** | System.IO.Compression | *(built-in)* | Compression | `DeflateStream`, `GZipStream` for compression; ZIP support includes deflate |

#### Testing Packages
- **xunit** (v2.4.2+) - Unit testing framework
- **xunit.runner.visualstudio** - Test runner for Visual Studio / `dotnet test`
- **AwesomeAssertions** - Fluent assertion library (already in use)
- **BenchmarkDotNet** (optional) - Performance benchmarking to compare with C++ version

#### Development Tools
- **Visual Studio 2022** or **JetBrains Rider** or **VS Code with C# DevKit**
- **.NET SDK 6.0+** for building and testing
- **Unsafe code enabled**: `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` in csproj

#### Platform Support
- **Windows**: Native support, file paths use backslash
- **Linux**: Full support via .NET runtime
- **macOS**: Full support via .NET runtime
- **Note**: Use `Path.Combine()` and `Path.DirectorySeparatorChar` for cross-platform paths

#### Key .NET BCL Features Leveraged
- `Span<T>` / `ReadOnlySpan<char>` - Zero-copy string slicing (performance critical)
- `Memory<T>` / `ArrayPool<T>` - Buffer pooling to reduce GC pressure
- `stackalloc` - Stack-based allocations for small temporary buffers
- `unsafe` code - Direct pointer manipulation in regex engine hot paths
- `Interlocked` - Lock-free atomic operations for thread-safe ID assignment
- `Stream` abstraction - Unified I/O interface (file, ZIP, memory, embedded resources)

#### Migration Risk Assessment

| Dependency | Risk Level | Mitigation |
|------------|------------|------------|
| ICU → .NET Unicode | **LOW** | .NET UTF-16 strings match C++ 16-bit character units; Unicode category APIs equivalent |
| libxml2 → XmlReader | **MEDIUM** | Entity resolution requires careful configuration; DTD processing must be enabled; validate with real HRC files |
| minizip → ZipArchive | **LOW** | .NET ZIP APIs mature and well-tested; straightforward mapping |
| Regex engine | **LOW** | ✅ Already ported in Phase 1 with full test coverage |
| Performance | **MEDIUM** | Profile early; use `Span<T>`, unsafe code, and pooling; target within 2x of C++ performance |

## Testing Infrastructure

Located in: `native/tests/`

**Test Types**:
1. **Unit Tests** (`native/tests/unit/`)
   - Environment tests
   - Exception handling tests
   - Component isolation tests

2. **Performance Tests** (`native/tests/performance/`)
   - Speed benchmarks
   - Parse performance metrics

3. **Integration Tests** (`native/tests/hrd_mapper/`)
   - HRD mapper functionality

**Test Framework**: Catch2 (included in `native/external/catch2/`)

---

### .NET Testing Migration Plan

#### Test Organization Structure
```
Far.Colorer.Tests/
├── Data/                              # Test data copied from native
│   ├── base/                          # Reference HRC/HRD files
│   │   ├── hrc/                       # → Copy from native/data/base/hrc/
│   │   ├── hrd/                       # → Copy from native/data/base/hrd/
│   │   └── catalog.xml                # → Copy from native/data/base/
│   └── tests/                         # Test input files
│       ├── cpp/                       # → Copy from native/data/tests/test/cpp/
│       ├── cmake/                     # → Copy from native/data/tests/test/cmake/
│       └── [other language tests]
├── Unit/                              # Component unit tests
│   ├── RegularExpressions/            # ✅ Already complete from Phase 1
│   ├── Regions/RegionTests.cs
│   ├── Parsers/
│   │   ├── Keywords/KeywordListTests.cs
│   │   ├── SchemeNodes/SchemeNodeTests.cs
│   │   ├── SchemeImplTests.cs
│   │   ├── HrcLibraryTests.cs
│   │   └── TextParserTests.cs
│   ├── Xml/XmlReaderTests.cs
│   └── IO/InputSourceTests.cs
├── Integration/                       # Cross-component tests
│   ├── HrcLoadingTests.cs             # Load real HRC files
│   ├── SimpleParsingTests.cs          # Parse small code snippets
│   └── RealWorldParsingTests.cs       # Full file parsing
└── Performance/                       # Benchmarks
    ├── RegexBenchmarks.cs             # ✅ Already exists
    ├── ParserBenchmarks.cs            # Parse performance vs C++
    └── MemoryBenchmarks.cs            # GC pressure measurement
```

#### Test Migration Strategy

##### 1. Unit Tests - Component Parity
**Goal**: Each C# component passes equivalent tests to C++ unit tests

| C++ Test | .NET Test | Migration Approach | Priority |
|----------|-----------|-------------------|----------|
| `native/tests/unit/test_encoding.cpp` | `Unit/Common/EncodingTests.cs` | Verify UTF-16 behavior matches C++ | P1 - Week 1 |
| `native/tests/unit/test_exceptions.cpp` | `Unit/Common/ExceptionTests.cs` | Exception message and type parity | P1 - Week 1 |
| *(regex tests)* | `Unit/RegularExpressions/*` | ✅ Already complete | ✅ Done |
| *(to be identified)* | `Unit/Parsers/KeywordListTests.cs` | Binary search, case sensitivity | P1 - Week 2 |
| *(to be identified)* | `Unit/Parsers/SchemeNodeTests.cs` | Region assignment, priority | P1 - Week 3 |

**Validation**: Each .NET unit test must be traceable to either:
- A corresponding C++ unit test (behavior parity)
- A specific requirement in HRC specification
- An edge case discovered during porting

##### 2. Integration Tests - Golden Data Validation
**Goal**: .NET parser produces identical output to C++ parser on real files

**Approach**:
1. **Extract C++ parser output** as golden reference:
   ```bash
   # Run C++ colorer on test file, save region output
   ./colorer_test --file test.cpp --output regions.json
   ```

2. **Run .NET parser** on same file:
   ```csharp
   var parser = factory.CreateTextParser();
   parser.SetFileType(fileType);
   parser.Parse(lineSource, regionHandler);
   var actualRegions = regionHandler.GetRegions();
   ```

3. **Compare outputs**:
   - Region boundaries (line, start, end positions)
   - Region types (names must match)
   - Nesting structure (enter/leave scheme events)

**Test Files** (from `native/data/tests/test/`):
| File | Language | Lines | Complexity | Priority |
|------|----------|-------|------------|----------|
| `cpp/testcases.c` | C | ~200 | Medium | P1 - Week 5 |
| `cmake/1/CMakeLists.txt` | CMake | ~50 | Low | P2 - Week 6 |
| `java/test.java` | Java | ~150 | Medium | P2 - Week 7 |
| `xml/test.xml` | XML | ~100 | Low | P2 - Week 7 |
| `perl/test.pl` | Perl | ~120 | High (regex heavy) | P3 - Week 8 |

**Acceptance Criteria**:
- ✅ At least **5 real-world test files** parse with **100% region match** to C++ output
- ✅ All files from `native/data/tests/test/cpp/` directory parse without errors
- ✅ No regressions when updating HRC files

##### 3. Performance Tests - Benchmarking
**Goal**: .NET parser performs within 2x of C++ parser speed

**Benchmark Scenarios**:
| Scenario | Target | Measurement |
|----------|--------|-------------|
| Small file (100 lines) | <5ms | Parse time |
| Medium file (1,000 lines) | <50ms | Parse time |
| Large file (10,000 lines) | <500ms | Parse time |
| Memory per cached line | <100 bytes | Heap allocation |
| GC pressure per 1,000 lines | <1MB | Gen0 collections |

**Tools**:
- **BenchmarkDotNet** for accurate microsecond timing
- **PerfView** / **dotnet-counters** for memory profiling
- Compare against C++ benchmarks from `native/tests/performance/`

##### 4. Continuous Validation
**Automated Checks** (CI pipeline):
1. All unit tests pass (>80% code coverage)
2. Integration tests with golden data match 100%
3. Performance tests within acceptable thresholds
4. No new compiler warnings
5. HRC file loading succeeds for all base types

**Cross-Language Verification**:
- **Tool**: Create a test harness that:
  1. Loads same HRC file in C++ and .NET
  2. Parses same source file
  3. Compares region output (JSON or XML format)
  4. Reports any differences

**Regression Testing**:
- Snapshot tests: Save expected parser output, detect unintended changes
- Version matrix: Test against multiple HRC file versions
- Platform matrix: Windows, Linux, macOS (all .NET supported platforms)

#### Test Data Management
- **DO NOT** duplicate test data - symlink or copy from `native/data/` during build
- **Build step**: Copy `native/data/base/` to `Far.Colorer.Tests/Data/base/`
- **Git**: Exclude copied data, only track native originals
- **Updates**: When native data changes, re-copy to keep in sync

#### Success Metrics
- ✅ **>80% code coverage** for new C# code
- ✅ **100% region match** on at least 5 real source files vs C++ parser
- ✅ **All HRC files load** from `data/base/hrc/base/` without errors
- ✅ **Performance within 2x** of C++ on benchmark suite
- ✅ **Zero correctness regressions** when updating dependencies or refactoring

## Code Statistics

- **C++ Source Files**: 54 files in `native/src/`
- **Regular Expression Engine**: ~1546 lines
- **Test Infrastructure**: Comprehensive unit and performance tests

## Key Technical Challenges for .NET Port

### 1. Regular Expression Engine
**Challenge**: The custom regex engine (~1546 lines) has Colorer-specific features not available in .NET's `System.Text.RegularExpressions`:
- Cross-pattern backreferences (`\yN`, `\y{name}`)
- Custom metacharacters (`~`, `\m`, `\M`)
- Unicode character class operations (intersection, subtraction)

**Options**:
- Port the entire regex engine to .NET (significant effort)
- Hybrid approach: use .NET regex where possible, custom implementation for special features
- Investigate if PCRE.NET or other libraries provide needed features

### 2. Unicode Handling
**Current**: 16-bit Unicode units (ICU library)
**Target**: .NET's native Unicode support (`System.String`, `System.Char`)
**Note**: UTF-16 compatible, should map well

### 3. XML Processing
**Current**: libxml2
**Target**: `System.Xml.Linq` (LINQ to XML) or `System.Xml`
**Note**: Straightforward migration

### 4. Performance Considerations
- Extensive use of pointers and manual memory management in C++
- .NET port should leverage:
  - `Span<T>` and `ReadOnlySpan<T>` for efficient string processing
  - `Memory<T>` for buffer management
  - Stackalloc for small allocations
  - String pooling where appropriate

### 5. File I/O and ZIP Support
**Current**: Custom InputSource abstraction + minizip
**Target**:
- `System.IO` for file operations
- `System.IO.Compression.ZipArchive` for ZIP support

## Risks and Mitigations

### High-Risk Areas

#### 1. TextParser Match Priority and Backtracking Logic
**Risk Level**: ⚠️ **HIGH**

**Description**: The core parsing loop in `TextParserImpl` has complex match prioritization rules:
- Multiple scheme nodes can match at same position
- `lowPriority` and `lowContentPriority` flags affect selection
- Backtracking behavior when no match found
- Stack-based scheme nesting with depth limits

**Impact**: Incorrect implementation produces wrong region boundaries, missing highlights, or infinite loops

**Mitigation**:
1. Port incrementally - one search method at a time (`searchKW`, `searchRE`, `searchBL`, `searchIN`)
2. Add extensive logging during development to trace match decisions
3. Create unit tests for each priority scenario before integration
4. **Golden data validation**: Compare every parse against C++ output on real HRC files
5. Add assertion checks for stack depth and backtracking limits
6. Code review with original C++ implementation side-by-side

**Timeline Impact**: Could add 1-2 weeks if not carefully managed

---

#### 2. Cross-Pattern Backreferences in Block End Regex
**Risk Level**: ⚠️ **HIGH**

**Description**: Block end regex can reference captures from block start regex via `\y2`, `\y{name}`:
```xml
<block start="/(?{StringEdge}(L|u8?)?&quot;)/" end="/(?{StringEdge}&quot;)/" .../>
```
The end pattern must match the same quote style as start.

**Impact**: Phase 1 regex engine supports this, but **integration** with TextParser matching requires careful SMatches state management

**Mitigation**:
1. ✅ Regex engine already validated with backreference tests in Phase 1
2. Create specific test cases for block matching with cross-pattern refs
3. Validate with real HRC examples (`c.hrc` string blocks, heredocs in `perl.hrc`)
4. Ensure `SMatches` objects are correctly passed between start/end regex matches
5. Add debug assertions that verify backreference indices are valid

**Timeline Impact**: Low if Phase 1 regex implementation is solid

---

#### 3. HRC XML Entity Resolution
**Risk Level**: ⚠️ **MEDIUM**

**Description**: HRC files use DTD entities for reusable patterns:
```xml
<!ENTITY c-unix "(?{Symbol}[&#x26;|^~])">
...
<regexp match="/&c-unix;/" region="Operator"/>
```

.NET `XmlReader` must expand these entities during parsing.

**Impact**: If entities not resolved, regex patterns will contain literal `&c-unix;` instead of expansion, causing regex compilation failures

**Mitigation**:
1. Configure `XmlReaderSettings.DtdProcessing = DtdProcessing.Parse`
2. Test entity resolution with `base.hrc` (defines many entities)
3. Create unit test that verifies entity content in parsed regex patterns
4. **Fallback**: If DTD processing problematic, pre-process HRC files to expand entities
5. Validate with all HRC files from `data/base/hrc/base/`

**Timeline Impact**: 1-3 days if DTD processing requires custom entity resolver

---

#### 4. VTList Virtual Entry Resolution
**Risk Level**: ⚠️ **MEDIUM**

**Description**: HRC supports virtual entries for scheme customization (overriding inherited nodes). The C++ implementation uses `VirtualEntry` and `VTList` which are complex.

**Impact**: Incorrect virtual entry resolution breaks scheme inheritance, causing wrong syntax highlighting in derived types

**Mitigation**:
1. Study C++ implementation (`VirtualEntry.h`, `SchemeImpl.cpp`) thoroughly
2. Document the resolution algorithm before coding
3. Add extensive unit tests for scheme inheritance scenarios
4. Test with real HRC inheritance chains (e.g., `cpp.hrc` inherits from `c.hrc`)
5. **Defer if needed**: Implement basic scheme loading first, add virtual entries in iteration 2

**Timeline Impact**: Could add 3-5 days; consider deferring to post-Phase 2

---

#### 5. Parse Cache Memory Management
**Risk Level**: ⚠️ **MEDIUM**

**Description**: C++ uses manual memory management for parse cache structures. .NET GC has different behavior:
- C++ caches are pointer-based linked lists
- .NET needs to avoid excessive Gen2 allocations
- Cache invalidation must prevent memory leaks

**Impact**: Poor cache design causes GC pressure, degrading performance to <<2x C++ speed

**Mitigation**:
1. Use object pooling (`ArrayPool<T>`, custom pools) for cache nodes
2. Profile early with PerfView to measure Gen0/Gen1/Gen2 allocations
3. Consider `struct` for small cache nodes (stack/inline allocation)
4. Implement cache trimming - discard old entries to bound memory usage
5. **Benchmark**: Compare cached vs non-cached parse performance regularly

**Performance Target**: <1MB GC pressure per 1,000 lines parsed

---

#### 6. Unicode Character Class Edge Cases
**Risk Level**: 🟡 **LOW-MEDIUM**

**Description**: HRC files use Unicode character classes like `[{L}]`, `[{Nd}]`, `[{ALL}]`, and set operations (intersection, subtraction). Phase 1 implemented this, but edge cases may exist.

**Impact**: Wrong character classification causes incorrect token boundaries (e.g., identifiers with Unicode chars)

**Mitigation**:
1. ✅ Phase 1 tests cover basic character classes
2. Add tests for HRC files with heavy Unicode use (e.g., non-Latin language highlighting)
3. Test set operations (`[{L}-[{Lu}]]` - letters except uppercase)
4. Validate with real-world files containing Unicode identifiers
5. Cross-reference .NET `char.GetUnicodeCategory()` with ICU categories

**Timeline Impact**: Minimal if Phase 1 implementation is complete

---

#### 7. Performance Degradation
**Risk Level**: 🟡 **MEDIUM**

**Description**: C# may be slower than C++ due to:
- GC pauses during parsing
- Virtual dispatch overhead (interfaces vs C++ pointers)
- Lack of fine-grained memory control

**Impact**: If >2x slower than C++, library may be unsuitable for large files or real-time editing

**Mitigation**:
1. Use `Span<T>` and `ReadOnlySpan<char>` aggressively (zero-copy)
2. Mark hot paths with `[MethodImpl(MethodImplOptions.AggressiveInlining)]`
3. Use `unsafe` code in critical loops (regex matching, scheme node iteration)
4. Profile with BenchmarkDotNet early and often
5. Seal classes where possible (enables devirtualization)
6. **Benchmark suite**: Run C++ and C# parsers on same files, compare results weekly

**Performance Targets**:
- Parse rate: >10,000 lines/sec
- Memory: <100 bytes/line cached
- Within 2x of C++ speed

---

#### 8. Cross-Platform Path Handling
**Risk Level**: 🟢 **LOW**

**Description**: C++ code may have Windows-specific path assumptions. .NET must work on Windows, Linux, macOS.

**Impact**: Catalog loading fails on non-Windows platforms if hardcoded backslashes exist

**Mitigation**:
1. Always use `Path.Combine()` for path construction
2. Use `Path.DirectorySeparatorChar` instead of hardcoded `/` or `\`
3. Test on Linux/macOS early (CI pipeline with multi-OS builds)
4. Validate ZIP loading works cross-platform (different path separators in ZIP entries)

**Timeline Impact**: Negligible if caught early

---

#### 9. Thread Safety in Region ID Assignment
**Risk Level**: 🟢 **LOW**

**Description**: C++ uses static counter for region IDs. .NET must handle concurrent HRC loading.

**Impact**: Race condition could assign duplicate region IDs, breaking array-based lookups

**Mitigation**:
1. Use `Interlocked.Increment()` for atomic ID assignment
2. Add unit test that loads HRC files concurrently
3. Document thread safety guarantees in `IHrcLibrary` interface

**Timeline Impact**: 1 day

---

### Risk Summary Table

| Risk Area | Level | Probability | Impact | Mitigation Cost | Priority |
|-----------|-------|-------------|--------|-----------------|----------|
| TextParser matching logic | HIGH | High | Critical | 1-2 weeks | P0 |
| Cross-pattern backreferences | HIGH | Medium | Critical | 3-5 days | P0 |
| HRC entity resolution | MEDIUM | Medium | High | 1-3 days | P1 |
| VTList virtual entries | MEDIUM | Low | High | 3-5 days | P2 (defer?) |
| Parse cache GC pressure | MEDIUM | High | High | Ongoing | P1 |
| Unicode edge cases | LOW-MED | Low | Medium | 1-2 days | P2 |
| Performance degradation | MEDIUM | Medium | High | Ongoing | P1 |
| Cross-platform paths | LOW | Low | Low | <1 day | P3 |
| Thread safety | LOW | Low | Medium | <1 day | P3 |

**Overall Risk Assessment**: **MEDIUM** - Manageable with careful planning and incremental testing

---

## Design Patterns Present

1. **Factory Pattern**: ParserFactory creates parser instances
2. **Strategy Pattern**: Different RegionHandler implementations
3. **Template Method**: Parser caching strategies
4. **Visitor Pattern**: Region processing
5. **Pimpl Idiom**: Implementation hiding (e.g., `TextParser::Impl`)

## Critical Files for Understanding

1. **Regex Engine**: `native/src/colorer/cregexp/cregexp.h`
2. **Parser Implementation**: `native/src/colorer/parsers/TextParserImpl.h`
3. **Factory**: `native/src/colorer/ParserFactory.h`
4. **HRC Example**: `data/base/hrc/base/c.hrc`
5. **HRD Example**: `data/base/hrd/console/black.hrd`

## Data File Compatibility

**Important**: The .NET port should maintain compatibility with existing HRC/HRD files to leverage the extensive syntax definition library already available.

## References

- Project repository: https://github.com/colorer/Colorer-library
- Documentation: https://colorer.github.io
- Original design is for C++14+ with optional ICU/legacy string handling
