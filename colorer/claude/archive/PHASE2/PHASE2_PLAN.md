# Colorer C# Port - Phase 2: Syntax Parsing Infrastructure

## Current Status
✅ **Completed**: Regular Expression Engine
- Custom regex implementation with Colorer-specific features
- Cross-pattern backreferences (`\yN`, `\y{name}`)
- Unicode character classes with set operations
- Comprehensive test coverage

## Key Findings from Phase 1

### Critical Success Factors
1. **Exact C++ Behavior**: Never guess, never shortcut - match C++ implementation exactly
2. **Test Against C++ Logic**: Tests must match how features are used in the library, not just pass
3. **Performance First**: Unsafe code is acceptable and necessary for performance-critical paths
4. **Real-World Validation**: Use actual HRC files and test data from `native/data/`

---

## Phase 2 Architecture Overview

The parsing system has the following dependency chain:
```
HRC Files → HrcLibrary → FileType → Scheme → SchemeNode → TextParser
                ↓                                              ↓
            Region System                              LineSource/RegionHandler
```

---

## Detailed Implementation Plan

### **Group 1: Foundation Classes** (Week 1)

**Testing Tasks for Group 1:**
- [ ] Day 1-2: Implement `Region.cs` + `RegionTests.cs` in parallel
- [ ] Day 2: Add parent hierarchy test cases (10+ scenarios)
- [ ] Day 3: Implement thread-safe ID assignment + concurrency test
- [ ] Day 3: Performance benchmarks (ID lookup, parent chain)
- [ ] Day 4: Code review with C++ comparison notes
- [ ] Day 5: Complete `ColorerException` + exception tests

#### 1.1 Region System (`Far.Colorer.Regions`)
**Files to port:**
- `native/src/colorer/Region.h` → `Region.cs`

**Responsibilities:**
- Region hierarchy (parent/child relationships)
- Region identity and comparison
- Quick ID-based lookups

**Key Implementation Details:**
- Immutable design (C++ has const everywhere)
- `hasParent()` recursion must match C++ exactly
- Region IDs are sequential and used for array indexing (performance critical)

**Testing Strategy:**
- Unit tests for parent hierarchy traversal
- Test with real region definitions from HRC files
- Performance tests for ID-based lookups

**Definition of Done:**
| Deliverable | Verification | Owner |
|-------------|--------------|-------|
| `Region.cs` class implemented | Code complete, compiles without warnings | Dev |
| Parent hierarchy traversal works | `RegionTests.cs` with 10+ test cases covering chain lookup | Dev |
| Thread-safe ID assignment | Unit test with concurrent region creation | Dev |
| Performance validated | ID lookup <10ns, parent chain <100ns (BenchmarkDotNet) | Dev |
| Code reviewed | PR approved, C++ comparison documented | Tech Lead |
| Documentation | XML docs on public API, architecture notes | Dev |

---

#### 1.2 Common Types and Utilities (`Far.Colorer.Common`)

**Files to port:**
- `native/src/colorer/Common.h` → String utilities (already have Character/CharacterClass)
- `native/src/colorer/Exception.h` → `ColorerException.cs` (expand existing)

**New Components:**
- `UnicodeString` wrapper (if needed, or use `string`/`ReadOnlySpan<char>`)
- Logging abstraction
- Constants and literals

**Design Decision Needed:**
- Use .NET `string` directly vs custom wrapper?
- Recommendation: Use `string` + `ReadOnlySpan<char>` for zero-copy operations
- Keep existing `Character`/`CharacterClass` from regex engine

**Definition of Done:**
| Deliverable | Verification | Owner |
|-------------|--------------|-------|
| `ColorerException` expanded with all error types | All C++ exception scenarios covered | Dev |
| String utility decisions documented | Architecture doc with rationale (string vs wrapper) | Tech Lead |
| Common constants ported | All C++ constants available in C# | Dev |
| Unit tests pass | `CommonTests.cs` with exception and utility tests | Dev |
| No external dependencies added | Uses only .NET BCL | Dev |

---

### **Group 2: Scheme Definition System** (Week 2-3)

**Testing Tasks for Group 2:**
- [ ] Week 2 Day 1: Implement `KeywordList.cs` + basic tests
- [ ] Week 2 Day 2: Add real keyword tests from `c.hrc` (load and match)
- [ ] Week 2 Day 3: Case sensitivity tests + symbol keyword tests
- [ ] Week 2 Day 4: Performance benchmarks for binary search
- [ ] Week 2 Day 5: SchemeNode base classes + region assignment tests
- [ ] Week 3 Day 1-2: Implement all SchemeNode types + unit tests
- [ ] Week 3 Day 3: Test priority ordering and lowPriority flags
- [ ] Week 3 Day 4: SchemeImpl integration + cross-node tests
- [ ] Week 3 Day 5: Validate with simple HRC scheme examples

#### 2.1 Keyword System (`Far.Colorer.Parsers.Keywords`)

**Files to port:**
- `native/src/colorer/parsers/KeywordList.h/cpp` → `KeywordList.cs`

**Responsibilities:**
- Keyword storage with case-sensitivity support
- Sorted list for binary search
- First character optimization
- Symbol vs word keyword distinction

**Key Implementation Details:**
- `sortList()` must use same comparison as C++
- `substrIndex()` optimization for substring matching
- Use `ReadOnlySpan<char>` for matching without allocations
- `minKeywordLength` optimization critical for performance

**Testing:**
- Test with real keyword lists from `c.hrc`
- Case-sensitive and case-insensitive matching
- Symbol keyword matching (`{`, `}`, etc.)
- Performance benchmarks vs C++ version

**Definition of Done:**
| Deliverable | Verification | Owner |
|-------------|--------------|-------|
| `KeywordList.cs` implemented | Binary search, case modes, first-char optimization | Dev |
| Sorting matches C++ | Unit test comparing sort order with C++ logic | Dev |
| Real HRC keyword tests | Load keywords from `c.hrc`, validate matching | QA |
| Performance acceptable | Binary search <100ns, within 1.5x of C++ | Dev |
| Span-based API | Uses `ReadOnlySpan<char>` for zero-copy matching | Dev |

---

#### 2.2 Scheme Nodes (`Far.Colorer.Parsers.SchemeNodes`)

**Files to port:**
- `native/src/colorer/parsers/SchemeNode.h` → Abstract `SchemeNode` hierarchy
- `native/src/colorer/parsers/VirtualEntry.h` → `VirtualEntry.cs`

**Class Hierarchy:**
```
SchemeNode (abstract)
├── SchemeNodeRegexp    - Single regex match with region assignment
├── SchemeNodeBlock     - Start/end regex pair with nested scheme
├── SchemeNodeKeywords  - Keyword matching with word boundaries
└── SchemeNodeInherit   - Inherit rules from another scheme
```

**Key Implementation Details:**
- `REGIONS_NUM` and `NAMED_REGIONS_NUM` arrays (must match `MATCHES_NUM` from regex)
- `lowPriority` and `lowContentPriority` flags affect match order
- `innerRegion` flag for block regions
- Virtual entries for scheme customization

**Critical C++ Behaviors:**
- Array sizes must match regex engine exactly
- Region assignment to both numbered and named captures
- Start/end regex interaction with backreferences

**Testing:**
- Parse simple scheme definitions
- Test region assignment to captures
- Test priority ordering
- Validate against actual HRC block/regexp nodes

---

#### 2.3 Scheme Implementation (`Far.Colorer.Parsers`)

**Files to port:**
- `native/src/colorer/Scheme.h` → `IScheme.cs` (interface)
- `native/src/colorer/parsers/SchemeImpl.h/cpp` → `SchemeImpl.cs`

**Responsibilities:**
- Container for `SchemeNode` vector
- Association with `FileType`
- Scheme name and lookup

**Key Implementation Details:**
- Nodes stored in order (execution order matters!)
- Reference to owning `FileType`
- Lazy initialization pattern

**Testing:**
- Create schemes programmatically
- Test node ordering preservation
- Validate with simple HRC examples

---

### **Group 3: File Type System** (Week 4)

#### 3.1 FileType (`Far.Colorer.Types`)

**Files to port:**
- `native/src/colorer/FileType.h` → `IFileType.cs` interface
- `native/src/colorer/parsers/FileTypeImpl.h/cpp` → `FileTypeImpl.cs`

**Responsibilities:**
- Type metadata (name, group, description)
- Parameter storage (`<param name="..." value="..." />`)
- Base scheme lazy loading
- Parameter value resolution (default vs user-set)

**Key Implementation Details:**
- Parameter system with default/user/description values
- `getParamValueInt()`, `getParamValueHex()` helpers
- Lazy `getBaseScheme()` - triggers loading on first access
- Pimpl pattern → internal implementation class

**Testing:**
- Parameter get/set with defaults
- Hex/int value parsing
- Test with real prototype parameters from HRC files

---

### **Group 4: HRC Library and XML Loading** (Week 5-6)

#### 4.1 XML Infrastructure (`Far.Colorer.Xml`)

**Files to port:**
- `native/src/colorer/xml/XmlReader.h` → `IXmlReader.cs`
- Use `System.Xml.Linq.XDocument` for implementation

**Responsibilities:**
- Abstract XML parsing interface
- Entity resolution for HRC includes (`&c-unix;`)
- Attribute access

**Key Differences from C++:**
- C++ uses libxml2, we use LINQ to XML
- Entity expansion must work identically
- Namespace handling for HRC schema

---

#### 4.2 HRC Library (`Far.Colorer.Parsers`)

**Files to port:**
- `native/src/colorer/HrcLibrary.h` → `IHrcLibrary.cs`
- `native/src/colorer/parsers/HrcLibraryImpl.h/cpp` → `HrcLibraryImpl.cs`
- `native/src/colorer/parsers/CatalogParser.h/cpp` → `CatalogParser.cs`

**Responsibilities:**
- Load `catalog.xml` to discover HRC files
- Parse HRC XML into `FileType`/`Scheme`/`SchemeNode` objects
- Manage region registry (all regions with IDs)
- Resolve scheme inheritance
- Entity resolution and includes

**Critical XML Parsing:**
HRC structure:
```xml
<hrc>
  <type name="c">
    <region name="String" parent="def:String"/>
    <scheme name="c">
      <keywords region="KeywordANSI">
        <word name="if"/>
        <word name="else"/>
      </keywords>
      <block start="/\/\*/" end="/\*\//" region="Comment"/>
      <inherit scheme="def:Comment"/>
    </scheme>
  </type>
</hrc>
```

**Key Implementation Details:**
- Region ID assignment (sequential, thread-safe)
- Scheme name resolution with `:` separator (`type:scheme`)
- `if`/`unless` attribute evaluation for parameters
- DTD entity expansion
- Error handling for malformed HRC

**Testing:**
- Load real HRC files: `c.hrc`, `cpp.hrc`, `default.hrc`
- Verify region hierarchy from `def` type
- Test scheme inheritance chains
- Validate entity resolution

**Definition of Done:**
| Deliverable | Verification | Owner |
|-------------|--------------|-------|
| `HrcLibraryImpl.cs` complete | Loads `catalog.xml`, creates FileTypes and Schemes | Dev |
| `CatalogParser.cs` functional | SAX-style XmlReader parsing with entity expansion | Dev |
| Entity resolution works | DTD entities expanded correctly (test with `base.hrc`) | Dev |
| All base HRC files load | Load all ~50 files from `data/base/hrc/base/` without errors | QA |
| Region hierarchy correct | `def:String` → `String` parent chain validated | Dev |
| Scheme inheritance works | `cpp.hrc` inherits from `c.hrc` correctly | QA |
| Integration test passes | Load catalog + parse "int x = 5;" → correct regions | QA |
| Performance validated | Catalog loading <500ms on standard hardware | Dev |

---

### **Group 5: Text Parser Input/Output** (Week 7)

#### 5.1 Line Source (`Far.Colorer.Parsing`)

**Files to port:**
- `native/src/colorer/LineSource.h` → `ILineSource.cs`

**Responsibilities:**
- Provide text lines to parser
- `startJob()`/`endJob()` lifecycle hooks
- `getLine(lineNumber)` for random access

**Implementation Options:**
- Interface for flexibility
- Implementations: `StringLineSource`, `FileLineSource`, `EditorLineSource`

**Testing:**
- Simple in-memory line source
- Multi-line text parsing
- Lifecycle hook invocation

**Definition of Done:**
| Deliverable | Verification | Owner |
|-------------|--------------|-------|
| `ILineSource` interface defined | Clean API matching C++ contract | Dev |
| `StringLineSource` implemented | In-memory string array implementation | Dev |
| Lifecycle hooks work | `startJob()`/`endJob()` called correctly | Dev |
| Unit tests pass | `LineSourceTests.cs` with 5+ scenarios | Dev |

---

#### 5.2 Region Handler (`Far.Colorer.Handlers`)

**Files to port:**
- Create `IRegionHandler.cs` interface (based on C++ virtual methods)
- `native/src/colorer/handlers/LineRegion.h/cpp` → `LineRegion.cs`
- `native/src/colorer/handlers/LineRegionsSupport.h/cpp` → `LineRegionsSupport.cs`

**Responsibilities:**
- Receive region events from parser:
  - `addRegion(line, start, end, region)` - simple region
  - `enterScheme(line, start, end, region)` - nested block start
  - `leaveScheme(line, start, end, region)` - nested block end

**Handler Implementations:**
- `LineRegionsSupport` - stores regions per line for editor display
- `LineRegionsCompactSupport` - memory-efficient version
- Custom handlers for different output formats

**Testing:**
- Capture regions from simple parsing
- Verify nested region boundaries
- Test scheme enter/leave pairing

---

### **Group 6: Core Parser** (Week 8-10)

#### 6.1 Parser Helpers (`Far.Colorer.Parsers`)

**Files to port:**
- `native/src/colorer/parsers/TextParserHelpers.h/cpp` → `TextParserHelpers.cs`

**Responsibilities:**
- Parse cache structures
- Virtual table list (VTList) for scheme customization
- `SMatches` already ported with regex engine

---

#### 6.2 Text Parser Implementation (`Far.Colorer.Parsers`)

**Files to port:**
- `native/src/colorer/TextParser.h` → `ITextParser.cs`
- `native/src/colorer/parsers/TextParserImpl.h/cpp` → `TextParserImpl.cs`

**Responsibilities:**
- Main parsing loop
- Three parse modes:
  - `CACHE_OFF` - Full parse
  - `CACHE_READ` - Use existing cache
  - `CACHE_UPDATE` - Incremental update
- Scheme node matching:
  - `searchKW()` - keyword matching
  - `searchRE()` - regex matching
  - `searchBL()` - block matching
  - `searchIN()` - inherit matching
- Stack-based scheme nesting (max depth 100)
- Region event generation

**Critical Implementation Details:**
- **Match priority**: Higher nodes in scheme definition match first UNLESS `lowPriority` is set
- **Backtracking**: Parser tries all scheme nodes at each position
- **Cross-pattern backreferences**: End regex can reference start regex captures via `\y2`, `\y{name}`
- **Cache invalidation**: Scheme changes invalidate cached parse trees
- **Performance**: Hot path, needs unsafe code and span-based matching

**Exact C++ Behavior Required:**
- Match selection when multiple nodes match at same position
- Priority handling with `lowPriority`/`lowContentPriority`
- Stack overflow protection (MAX_RECURSION_LEVEL)
- Breaking parse mid-stream (`breakParse()`)

**Testing:**
- Simple scheme with single keyword
- Nested blocks (comments, strings)
- Scheme inheritance
- Cross-pattern backreferences in blocks
- Cache modes (off, read, update)
- **Real-world validation**: Parse actual source files using real HRC definitions

**Definition of Done - CRITICAL MILESTONE:**
| Deliverable | Verification | Owner |
|-------------|--------------|-------|
| `TextParserImpl.cs` complete | All four search methods (`searchKW`, `searchRE`, `searchBL`, `searchIN`) | Dev |
| Match priority exact | Unit tests verify priority matches C++ behavior in all scenarios | Dev |
| Cross-pattern backrefs work | Block end regex correctly references start captures (`\y2`, `\y{name}`) | Dev |
| Cache implementation | All three modes (OFF, READ, UPDATE) functional | Dev |
| Stack safety | MAX_RECURSION_LEVEL enforced, no stack overflows | Dev |
| Parse simple C file | "int x = 5;" produces correct keyword/number regions | Dev + QA |
| Parse complex C file | `cpp/testcases.c` regions match C++ parser 100% | QA |
| Performance meets target | >10,000 lines/sec on typical code files | Dev |
| Memory target met | <100 bytes per cached line, <1MB GC per 1000 lines | Dev |
| 5+ real files validated | C, Java, XML, CMake, Perl files match C++ output 100% | QA |
| Integration tests green | `RealWorldParsingTests.cs` all passing | QA |
| Performance benchmarks | BenchmarkDotNet results within 2x of C++ | Dev |
| Code review complete | PR approved with C++ comparison notes | Tech Lead |

**Exit Criteria for Phase 2:**
- ✅ All DoD items above verified
- ✅ Zero correctness regressions vs C++
- ✅ All HRC files from `data/base/hrc/base/` load successfully
- ✅ Test coverage >80% for TextParser code

---

### **Group 7: Integration** (Week 11)

#### 7.1 Parser Factory (`Far.Colorer`)

**Files to port:**
- `native/src/colorer/ParserFactory.h` → `IParserFactory.cs`
- `native/src/colorer/parsers/ParserFactoryImpl.h/cpp` → `ParserFactoryImpl.cs`

**Responsibilities:**
- Load `catalog.xml` from directory or ZIP
- Create `HrcLibrary`
- Create `TextParser` instances
- Create HRD mappers (StyledHRDMapper, TextHRDMapper)

**Testing:**
- Load from `native/data/base/`
- Create parser for C file type
- Basic end-to-end parsing

---

## Incremental Integration Milestones

**Purpose**: Validate vertical slices early to surface integration issues before full TextParser completion.

### Milestone 1: "Hello Region" (End of Week 1)
**Goal**: Create and verify a single region programmatically

**Components Required**:
- ✅ `Region.cs` with ID assignment
- ✅ `ColorerException.cs` for error handling

**Test**:
```csharp
var region = new Region("test:Keyword", null, "desc");
Assert.That(region.Id).IsGreaterThan(0);
Assert.That(region.Name).IsEqualTo("test:Keyword");
```

**Success Criteria**: Region creation and hierarchy working

---

### Milestone 2: "Parse a Keyword" (End of Week 4)
**Goal**: Load a minimal HRC file with keywords, match a single keyword

**Components Required**:
- ✅ Region, Keywords, SchemeNodes
- ✅ FileType, Scheme
- ⚠️ Minimal HrcLibrary (stub, no full catalog loading yet)

**Test HRC**:
```xml
<hrc>
  <type name="test">
    <scheme name="test">
      <keywords region="Keyword">
        <word name="int"/>
      </keywords>
    </scheme>
  </type>
</hrc>
```

**Test Code**:
```csharp
var library = new HrcLibraryImpl();
library.LoadHrcFromString(minimalHrc);
var fileType = library.GetFileType("test");
var scheme = fileType.GetBaseScheme();
// Manually invoke keyword matching (no full parser yet)
var match = scheme.SearchKeyword("int");
Assert.That(match).IsNotNull();
```

**Success Criteria**: Keyword loading and matching works

---

### Milestone 3: "Load Real Catalog" (End of Week 6)
**Goal**: Load full `catalog.xml` and all base HRC files without errors

**Components Required**:
- ✅ All of Groups 1-4 (Region, Scheme, FileType, HrcLibrary)
- ✅ XML SAX parser with entity resolution
- ✅ Catalog parser

**Test**:
```csharp
var factory = new ParserFactoryImpl();
factory.LoadCatalog("data/base/catalog.xml");
var cFileType = factory.HrcLibrary.GetFileType("c");
Assert.That(cFileType).IsNotNull();
Assert.That(cFileType.GetBaseScheme()).IsNotNull();
```

**Success Criteria**:
- All ~50 HRC files load without errors
- Entity resolution works (`&c-unix;` expanded)
- Scheme inheritance chains correct (cpp → c)
- All regions have valid IDs

---

### Milestone 4: "Parse One Line" (End of Week 8)
**Goal**: Parse a single line of C code with TextParser (no caching)

**Components Required**:
- ✅ All components through Group 5 (LineSource, RegionHandler)
- ⚠️ TextParser with basic matching (no cache, no complex priority)

**Test**:
```csharp
var parser = factory.CreateTextParser();
parser.SetFileType(cFileType);
var lineSource = new StringLineSource(new[] { "int x = 5;" });
var handler = new LineRegionsSupport();
parser.SetLineSource(lineSource);
parser.SetRegionHandler(handler);
parser.Parse(0, 1, TextParseMode.CACHE_OFF);

var regions = handler.GetLineRegions(0);
Assert.That(regions).HasCount(3); // "int" (keyword), "x" (identifier), "5" (number)
```

**Success Criteria**:
- Simple regex, keyword, and block matching works
- Region boundaries correct
- No crashes or infinite loops

---

### Milestone 5: "Parse Real File" (End of Week 10)
**Goal**: Parse complete C file with all features (cache, priority, backreferences)

**Components Required**:
- ✅ Full TextParser with caching
- ✅ All search methods implemented
- ✅ Cross-pattern backreferences

**Test**:
```csharp
var lines = File.ReadAllLines("data/tests/test/cpp/testcases.c");
var lineSource = new StringLineSource(lines);
parser.SetLineSource(lineSource);
parser.Parse(0, lines.Length, TextParseMode.CACHE_UPDATE);

// Compare regions with golden data from C++ parser
var expectedRegions = LoadGoldenData("testcases.c.regions.json");
var actualRegions = handler.GetAllRegions();
Assert.That(actualRegions).IsEqualTo(expectedRegions); // 100% match
```

**Success Criteria**:
- All lines parse without errors
- Regions match C++ output 100%
- Performance >10,000 lines/sec
- Memory usage acceptable

---

### Milestone 6: "Full Integration" (End of Week 11)
**Goal**: End-to-end workflow with ParserFactory

**Test**:
```csharp
var factory = ParserFactory.CreateFromDirectory("data/base/");
var parser = factory.CreateTextParser();
// Parse 5+ different language files (C, Java, XML, CMake, Perl)
// All must match C++ output 100%
```

**Success Criteria**: All Phase 2 acceptance criteria met

---

## Testing Strategy

### Unit Testing Approach

**Level 1: Isolated Component Tests**
- Each class tested independently with mocks
- Match C++ unit tests in `native/tests/unit/`

**Level 2: Integration Tests**
- Load real HRC files from `native/data/base/hrc/`
- Parse small code snippets
- Compare region output

**Level 3: Real-World Validation Tests**
- Use test files from `native/data/tests/test/`
- For example:
  - `native/data/tests/test/cpp/testcases.c`
  - `native/data/tests/test/cmake/1/CMakeLists.txt`
- Parse with C++ colorer and C# colorer
- Compare region boundaries and types
- **Critical**: Don't adjust tests to pass, adjust code to match C++

**Level 4: Performance Benchmarks**
- Match or exceed C++ parser performance
- Target: Parse 10,000 lines/sec minimum
- Memory usage comparable to C++

### Test Data Organization
```
net/
  Far.Colorer.Tests/
    Data/                       # Copy from native/data/
      base/
        hrc/                    # Real HRC files
        hrd/                    # Real HRD files
        catalog.xml
      tests/
        test/cpp/               # Real source files
        test/cmake/
    Regions/
      RegionTests.cs
    Parsers/
      Keywords/
        KeywordListTests.cs
      SchemeNodes/
        SchemeNodeTests.cs
      SchemeImplTests.cs
      HrcLibraryTests.cs
      TextParserTests.cs
    Integration/
      RealWorldParsingTests.cs  # End-to-end with real files
```

---

## Key Validation Points

### Must Match C++ Exactly
1. **Region hierarchy resolution** - parent chain lookup
2. **Keyword sorting** - binary search must find same matches
3. **Scheme node matching order** - priority handling
4. **Block end regex backreferences** - `\y2` referencing start captures
5. **Stack depth limits** - MAX_RECURSION_LEVEL behavior
6. **Unicode handling** - same character classification

### Performance Critical Paths
1. Keyword binary search - use `Span<char>` comparison
2. Regex matching - already optimized in Phase 1
3. Region array lookups - use ID-based indexing
4. Line iteration - avoid string allocations
5. Cache structures - minimize GC pressure

---

## Dependencies and Build

### NuGet Packages
```xml
<PackageReference Include="System.IO.Compression" />        <!-- ZIP support -->
```

Keep existing:
- `xunit` for testing
- `AwesomeAssertions` for fluent assertions

### Project Structure
```
Far.Colorer/
  Common/
    ColorerException.cs
  Regions/
    Region.cs
  Types/
    IFileType.cs
    FileTypeImpl.cs
  Parsers/
    Keywords/
      KeywordList.cs
      KeywordInfo.cs
    SchemeNodes/
      SchemeNode.cs
      SchemeNodeRegexp.cs
      SchemeNodeBlock.cs
      SchemeNodeKeywords.cs
      SchemeNodeInherit.cs
      VirtualEntry.cs
    SchemeImpl.cs
    IScheme.cs
    HrcLibraryImpl.cs
    IHrcLibrary.cs
    TextParserImpl.cs
    ITextParser.cs
    TextParserHelpers.cs
  Xml/
    IXmlReader.cs
    XDocumentReader.cs
  IO/
    ILineSource.cs
    StringLineSource.cs
  Handlers/
    IRegionHandler.cs
    LineRegion.cs
    LineRegionsSupport.cs
  ParserFactory.cs
  IParserFactory.cs
  RegularExpressions/  # Already complete
```

---

## Risk Mitigation

### High-Risk Areas
1. **TextParser matching logic** - Most complex, needs exact C++ behavior
   - Mitigation: Port incrementally, test each search method independently

2. **HRC XML parsing** - Entity resolution, inheritance
   - Mitigation: Test with simplest HRC first (`empty.hrc`), then build up

3. **Performance degradation** - C# slower than C++
   - Mitigation: Profile early, use `Span<T>`, unsafe code where needed

### Unknowns
1. Cache structure performance in C# vs C++ pointers
   - Plan: Implement simple version first, optimize later

2. VTList virtual entry resolution complexity
   - Plan: Study C++ implementation carefully, add extensive logging

---

## Non-Functional Requirements Checklist

**Purpose**: Engineers must verify these requirements before each PR merge. This ensures quality stays visible throughout implementation.

### Performance Requirements

**Parser Speed:**
- [ ] Small files (100 lines): <5ms parse time
- [ ] Medium files (1,000 lines): <50ms parse time
- [ ] Large files (10,000 lines): <500ms parse time
- [ ] Overall target: >10,000 lines/sec on typical code
- [ ] Within 2x of C++ parser speed on same hardware

**Memory Efficiency:**
- [ ] Memory per cached line: <100 bytes average
- [ ] GC pressure: <1MB Gen0 allocations per 1,000 lines parsed
- [ ] No Gen2 collections during normal parsing operations
- [ ] Object pooling used for hot-path allocations
- [ ] No memory leaks (validate with long-running parse sessions)

**Component-Specific Benchmarks:**
- [ ] Region ID lookup: <10ns
- [ ] Region parent chain traversal: <100ns
- [ ] Keyword binary search: <100ns
- [ ] Regex match (simple pattern): <1µs
- [ ] HRC catalog loading: <500ms for full base catalog

### Correctness Requirements

**Behavioral Parity with C++:**
- [ ] Region hierarchy traversal matches C++ exactly
- [ ] Keyword sorting produces identical order to C++
- [ ] Scheme node priority matching exact (including `lowPriority` flags)
- [ ] Cross-pattern backreferences (`\y2`, `\y{name}`) work correctly
- [ ] Block end regex can reference start regex captures
- [ ] Stack depth limits enforced (MAX_RECURSION_LEVEL)
- [ ] Cache invalidation behavior matches C++

**Golden Data Validation:**
- [ ] At least 5 real-world files parse with 100% region match vs C++
- [ ] Region boundaries (line, start, end) identical to C++
- [ ] Region types (names) identical to C++
- [ ] Nesting structure (enter/leave scheme) identical to C++
- [ ] No differences in region output for test files

**HRC Compatibility:**
- [ ] All ~50 HRC files from `data/base/hrc/base/` load without errors
- [ ] DTD entity resolution works (`&c-unix;`, `&regexp;`, etc.)
- [ ] Scheme inheritance chains resolve correctly (e.g., cpp → c)
- [ ] Parameter substitution in HRC files works
- [ ] Virtual entries (if implemented) resolve correctly

### Code Quality Requirements

**Testing:**
- [ ] Unit test coverage >80% for new code
- [ ] All public APIs have XML documentation comments
- [ ] Integration tests cover vertical slices (milestones 1-6)
- [ ] Performance benchmarks included (BenchmarkDotNet)
- [ ] No test marked `[Skip]` without issue number and justification

**Code Standards:**
- [ ] No compiler warnings
- [ ] Nullable reference types enabled and annotations correct
- [ ] `unsafe` code only in approved hot paths (regex matching, parsing loops)
- [ ] All `unsafe` blocks have justification comments
- [ ] No magic numbers (use named constants)
- [ ] Error messages clear and actionable

**Architecture:**
- [ ] Follows established patterns from C++ (Factory, Strategy, etc.)
- [ ] Interfaces defined for extension points (ILineSource, IRegionHandler)
- [ ] Immutable objects where C++ uses const
- [ ] No static mutable state (except controlled singletons)
- [ ] Thread safety documented on all public APIs

### Platform Compatibility Requirements

**Cross-Platform:**
- [ ] Uses `Path.Combine()` for all path construction
- [ ] No hardcoded path separators (`/` or `\`)
- [ ] Tests pass on Windows, Linux, macOS (CI validation)
- [ ] ZIP file paths handled correctly cross-platform
- [ ] No platform-specific P/Invoke without abstraction

**Runtime:**
- [ ] Targets .NET 6.0 or higher
- [ ] No dependencies on deprecated APIs
- [ ] Runs on x64 and ARM64 architectures
- [ ] No assumptions about endianness

### Security Requirements

**Input Validation:**
- [ ] HRC files validated against expected schema
- [ ] Malformed XML handled gracefully (exception, not crash)
- [ ] Regex patterns validated (no catastrophic backtracking)
- [ ] File paths sanitized (no directory traversal via ZIP entries)
- [ ] Stack overflow protection in recursive parsing

**Resource Limits:**
- [ ] Maximum recursion depth enforced (MAX_RECURSION_LEVEL)
- [ ] Large files don't cause unbounded memory growth
- [ ] Timeout mechanisms for long-running operations (optional)

### Maintainability Requirements

**Documentation:**
- [ ] Architecture decisions documented (ADRs or inline)
- [ ] Complex algorithms have explanation comments
- [ ] C++ equivalents noted for ported code (e.g., `// From cregexp.cpp:342`)
- [ ] Known deviations from C++ documented with rationale
- [ ] Public APIs have usage examples in XML comments

**Code Organization:**
- [ ] Namespace structure matches architecture (Far.Colorer.*)
- [ ] File names match class names
- [ ] One public type per file (unless nested types)
- [ ] Test files mirror source structure (Unit/Parsers/KeywordListTests.cs)

**Debugging:**
- [ ] Logging hooks available for diagnostics
- [ ] ToString() overrides on key types for debugging
- [ ] Debug assertions for invariants (Debug.Assert)
- [ ] No swallowed exceptions (log or rethrow)

---

### Pre-Merge Checklist (Copy for Each PR)

**Before submitting PR:**
- [ ] All relevant items from Non-Functional Checklist verified
- [ ] Unit tests added for new code
- [ ] Performance benchmarks run (if hot path changes)
- [ ] No new compiler warnings
- [ ] Code reviewed against C++ implementation
- [ ] Documentation updated (XML comments, architecture notes)
- [ ] Integration tests pass (if applicable)

**For critical components (TextParser, HrcLibrary):**
- [ ] Golden data validation performed
- [ ] Performance regression check vs previous commit
- [ ] Memory profiling completed (PerfView/dotnet-counters)
- [ ] Cross-platform CI passed

---

## Acceptance Criteria

✅ **Phase 2 Complete When:**
1. All C++ classes ported with equivalent functionality
2. Can load `c.hrc` and parse a simple C file
3. Region boundaries match C++ parser output
4. All real HRC files from `native/data/base/hrc/base/` load without errors
5. Performance within 2x of C++ version
6. Test coverage >80% for all new code
7. At least 5 real-world test files parse identically to C++

---

## Timeline Estimate

- **Weeks 1-6**: Core infrastructure (Regions → HrcLibrary)
- **Weeks 7-10**: Parser implementation
- **Week 11**: Integration and validation
- **Total**: ~11 weeks for full Phase 2

### Staffing Assumptions

**Team Composition:**
- **2 Senior Developers**: Core implementation (TextParser, HrcLibrary, regex integration)
  - Developer 1: Focus on parser infrastructure (Groups 1-5)
  - Developer 2: Focus on TextParser implementation (Group 6)
- **1 QA Engineer**: Testing, validation, C++ parity verification
  - Week 1-5: Prepare test infrastructure and golden data
  - Week 6-11: Validate each component, run integration tests
- **1 Tech Lead**: Part-time (25%) - architecture decisions, code reviews, risk management

**Effort Breakdown:**
| Week | Dev 1 | Dev 2 | QA | Tech Lead |
|------|-------|-------|-----|-----------|
| 1 | Region, Common | Test infra setup | Prepare test data | Architecture review |
| 2-3 | Keywords, SchemeNodes | SchemeImpl | Unit test scaffolding | Design review |
| 4 | FileType | FileType support | Parameter validation | - |
| 5-6 | HrcLibrary, XML | Catalog parser | HRC loading tests | Entity resolution review |
| 7 | LineSource, Handlers | TextParser helpers | Handler validation | - |
| 8-9 | Support TextParser | TextParser core | Unit testing | Match priority review |
| 10 | Performance tuning | Performance tuning | Real-world validation | Performance review |
| 11 | ParserFactory | Integration | Full integration tests | Final sign-off |

**Total Effort:**
- Developers: 2 × 11 weeks = 22 developer-weeks
- QA: 1 × 11 weeks = 11 QA-weeks
- Tech Lead: 0.25 × 11 weeks = 2.75 lead-weeks
- **Total**: 35.75 person-weeks

**Parallelization Opportunities:**
- Groups 1-2 can proceed together (Dev 1 + Dev 2 in parallel)
- XML/IO (4.1, 5.1) can be done in parallel with FileType (3.1)
- Unit testing happens concurrently with implementation
- Dev 2 can start TextParser design while Dev 1 finishes HrcLibrary

**Critical Path:**
1. Region System (1 week) → Blocks Groups 2-7
2. Scheme System (2-3 weeks) → Blocks Groups 4, 6
3. HrcLibrary (5-6 weeks) → Blocks Group 6
4. TextParser (8-10 weeks) → Blocks Group 7

**Risk Buffer:**
- 20% buffer (2 weeks) for unforeseen issues
- **Worst case**: 13 weeks with mitigation
- **Best case**: 10 weeks if parallelization optimal

---
