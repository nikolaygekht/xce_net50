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

---

### **Group 2: Scheme Definition System** (Week 2-3)

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

Can be parallelized:
- Groups 1-2 can proceed together
- XML/IO (4.1, 5.1) can be done in parallel with FileType (3.1)

---
