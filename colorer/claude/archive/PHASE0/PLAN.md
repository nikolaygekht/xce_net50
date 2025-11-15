# Colorer-Library .NET Port - Implementation Plan

## Overview

This document outlines the comprehensive plan for porting the Colorer syntax highlighting library from C++ to .NET, with emphasis on performance, maintainability, and compatibility with existing HRC/HRD syntax definition files.

## Goals

1. **Functional Compatibility**: Support all existing HRC/HRD syntax files without modification
2. **Performance**: Achieve comparable or better performance than C++ version using .NET optimizations
3. **Modern .NET**: Target .NET 8+ (LTS) with modern C# practices
4. **Maintainability**: Clean, well-tested, idiomatic .NET code
5. **Unicode-Only**: Focus exclusively on Unicode (UTF-16), no codepage support needed

## Technology Stack

### Target Framework
- **.NET 8.0** (LTS) or later
- **C# 12** language features

### Core Libraries
- **System.Text.RegularExpressions** - Base regex support
- **System.Xml.Linq** - XML parsing for HRC/HRD files
- **System.IO.Compression** - ZIP archive support
- **System.Buffers** - Memory pooling
- **System.Memory** - Span<T> support

### Testing Framework
- **xUnit** - Primary test framework
- **FluentAssertions** - Assertion library
- **BenchmarkDotNet** - Performance benchmarking

### Build & Tooling
- **MSBuild** / **.NET SDK**
- **GitHub Actions** - CI/CD
- **SonarAnalyzer** - Code quality
- **Coverlet** - Code coverage

## Project Structure

```
Colorer.NET/
├── src/
│   ├── Colorer.Core/
│   │   ├── RegularExpressions/        # Custom regex engine
│   │   │   ├── ColorerRegex.cs
│   │   │   ├── RegexNode.cs
│   │   │   ├── RegexMatcher.cs
│   │   │   ├── Operators/
│   │   │   └── CharacterClasses/
│   │   ├── Parsers/                    # Text parsing
│   │   │   ├── TextParser.cs
│   │   │   ├── TextParserImpl.cs
│   │   │   ├── ParseCache.cs
│   │   │   └── SchemeImpl.cs
│   │   ├── Hrc/                        # HRC processing
│   │   │   ├── HrcLibrary.cs
│   │   │   ├── FileType.cs
│   │   │   ├── Scheme.cs
│   │   │   └── SchemeNode/
│   │   ├── Hrd/                        # HRD processing
│   │   │   ├── StyledHrdMapper.cs
│   │   │   ├── TextHrdMapper.cs
│   │   │   └── RegionMapper.cs
│   │   ├── Regions/                    # Region system
│   │   │   ├── Region.cs
│   │   │   ├── RegionDefine.cs
│   │   │   └── RegionHandler.cs
│   │   ├── IO/                         # I/O abstractions
│   │   │   ├── IInputSource.cs
│   │   │   ├── FileInputSource.cs
│   │   │   └── ZipInputSource.cs
│   │   ├── Xml/                        # XML utilities
│   │   │   ├── XmlReader.cs
│   │   │   └── CatalogParser.cs
│   │   ├── ParserFactory.cs
│   │   └── Common/
│   │       ├── UnicodeString.cs (if needed)
│   │       └── Exceptions.cs
│   └── Colorer.Editor/                 # Optional: Editor integration
│       └── BaseEditor.cs
├── tests/
│   ├── Colorer.Tests.Unit/
│   │   ├── RegularExpressions/
│   │   ├── Parsers/
│   │   ├── Hrc/
│   │   └── Hrd/
│   ├── Colorer.Tests.Integration/
│   │   └── EndToEnd/
│   └── Colorer.Tests.Performance/
│       └── Benchmarks/
├── data/                               # HRC/HRD files (symlink or copy)
└── samples/
    └── Colorer.Samples.Console/
```

## Implementation Phases

### Phase 1: Foundation & Infrastructure (Weeks 1-2)

#### 1.1 Project Setup
- [ ] Create solution structure
- [ ] Configure build system (.csproj, Directory.Build.props)
- [ ] Setup CI/CD pipeline (GitHub Actions)
- [ ] Configure code analysis tools
- [ ] Setup test projects with frameworks

#### 1.2 Core Data Structures
- [ ] Implement `Region` class
  - Region hierarchy and inheritance
  - Region naming and identification
- [ ] Implement `FileType` class
  - File type detection
  - Scheme association
- [ ] Implement exception hierarchy
  - `ColorerException` base class
  - `ParserFactoryException`
  - `RegexException`

#### 1.3 I/O Abstraction Layer
- [ ] Define `IInputSource` interface
- [ ] Implement `FileInputSource`
- [ ] Implement `ZipInputSource` using `System.IO.Compression`
- [ ] Create input source factory
- [ ] Write unit tests for I/O layer

**Deliverables**:
- Buildable solution with basic structure
- Core interfaces and base classes
- I/O abstraction with tests
- CI/CD pipeline running

### Phase 2: XML Processing (Week 3)

#### 2.1 XML Infrastructure
- [ ] Implement `XmlReader` wrapper around `System.Xml.Linq`
- [ ] Create `CatalogParser` for catalog.xml
- [ ] Support entity resolution for HRC files
- [ ] Handle XML namespaces and schema validation

#### 2.2 Catalog Processing
- [ ] Parse `catalog.xml` structure
- [ ] Load HRC file locations
- [ ] Load HRD file locations
- [ ] Handle catalog inheritance and includes

**Deliverables**:
- XML parsing infrastructure
- Catalog.xml loading capability
- Unit tests for XML processing

### Phase 3: Regular Expression Engine (Weeks 4-6)

**This is the most critical and complex phase**

#### 3.1 Analysis & Design
- [ ] Analyze all regex features used in HRC files
- [ ] Document features not available in .NET regex
- [ ] Design hybrid approach:
  - Use `System.Text.RegularExpressions.Regex` for standard patterns
  - Custom implementation for Colorer-specific features
- [ ] Create regex feature compatibility matrix

#### 3.2 Core Regex Engine
- [ ] Implement `ColorerRegex` main class
  - Pattern compilation
  - Match execution
  - Backreference tracking
- [ ] Implement `RegexNode` tree structure
  - Node types for all operators
  - Tree building from pattern string
  - Tree optimization
- [ ] Implement `RegexMatcher` execution engine
  - Stack-based (non-recursive) matching
  - Match state management
  - Backtracking support

#### 3.3 Standard Operators
- [ ] Basic quantifiers: `*`, `+`, `?`, `{n,m}`
- [ ] Non-greedy quantifiers: `*?`, `+?`, `??`, `{n,m}?`
- [ ] Alternation: `|`
- [ ] Grouping: `(...)`
- [ ] Named groups: `(?{name} ...)`
- [ ] Non-capturing groups: `(?{} ...)`

#### 3.4 Character Classes
- [ ] Unicode general categories: `[{L}]`, `[{Nd}]`, etc.
- [ ] Special classes: `[{ALL}]`, `[{ASSIGNED}]`, `[{UNASSIGNED}]`
- [ ] Character class union: `[{Lu}[{Ll}]]`
- [ ] Character class intersection: `[{ALL}&&[{L}]]`
- [ ] Character class subtraction: `[{ASSIGNED}-[{Lu}]]`
- [ ] Standard classes: `\d`, `\D`, `\w`, `\W`, `\s`, `\S`
- [ ] Unicode escapes: `\x{2028}`, `\x0A`

#### 3.5 Metacharacters
- [ ] `.` - Any character
- [ ] `^` - Start of line
- [ ] `$` - End of line
- [ ] `~` - Start of scheme (Colorer-specific)
- [ ] `\b`, `\B` - Word boundaries
- [ ] `\m`, `\M` - Set start/end markers (Colorer-specific)

#### 3.6 Lookahead/Lookbehind
- [ ] `?=` - Positive lookahead
- [ ] `?!` - Negative lookahead
- [ ] `?#N` - Positive lookbehind (N characters)
- [ ] `?~N` - Negative lookbehind (N characters)

#### 3.7 Backreferences
- [ ] Standard backreferences: `\1`, `\2`, etc.
- [ ] Named backreferences: `\p{name}`
- [ ] Cross-pattern backreferences: `\yN`, `\YN` (Colorer-specific)
- [ ] Named cross-pattern: `\y{name}`, `\Y{name}` (Colorer-specific)

#### 3.8 Performance Optimization
- [ ] Use `Span<char>` for string processing
- [ ] Implement regex compilation/caching
- [ ] Profile and optimize hot paths
- [ ] Consider `ArrayPool<T>` for temporary allocations
- [ ] Implement first-character optimization
- [ ] Boyer-Moore for literal string searches

#### 3.9 Testing
- [ ] Unit tests for each operator type
- [ ] Tests for Unicode character classes
- [ ] Backreference tests
- [ ] Complex pattern integration tests
- [ ] Performance benchmarks vs C++ version
- [ ] Test with real HRC file patterns

**Deliverables**:
- Complete custom regex engine
- Full test coverage (>90%)
- Performance benchmarks
- Documentation of regex syntax

### Phase 4: HRC Processing (Weeks 7-8)

#### 4.1 HRC Data Model
- [ ] Implement `Scheme` class
- [ ] Implement `SchemeNode` hierarchy:
  - `SchemeNodeBlock` - Block matching
  - `SchemeNodeRegexp` - Regex matching
  - `SchemeNodeKeywords` - Keyword matching
  - `SchemeNodeInherit` - Scheme inheritance
- [ ] Implement scheme inheritance resolution
- [ ] Implement virtual scheme support

#### 4.2 HRC Parser
- [ ] Parse HRC XML files
- [ ] Build scheme tree structure
- [ ] Resolve scheme references
- [ ] Handle entity includes
- [ ] Process region definitions
- [ ] Validate HRC structure

#### 4.3 HRC Library
- [ ] Implement `HrcLibrary` class
- [ ] Load multiple HRC files
- [ ] Manage FileType catalog
- [ ] Provide FileType lookup by name
- [ ] Handle scheme caching

**Deliverables**:
- HRC loading and parsing
- Scheme data structures
- Unit tests with sample HRC files

### Phase 5: HRD Processing (Week 9)

#### 5.1 HRD Data Model
- [ ] Implement `RegionDefine` class
  - Foreground/background colors
  - Font styles
- [ ] Implement color representation
  - Console colors
  - RGB colors

#### 5.2 HRD Parser
- [ ] Parse HRD XML files
- [ ] Build region mappings
- [ ] Support console and styled modes

#### 5.3 Region Mappers
- [ ] Implement `StyledHrdMapper`
  - RGB color support
  - Font styles (bold, italic, etc.)
- [ ] Implement `TextHrdMapper`
  - Console color support
  - ANSI color codes
- [ ] Base `RegionMapper` interface

**Deliverables**:
- HRD loading and parsing
- Region mapper implementations
- Color scheme application

### Phase 6: Text Parser (Weeks 10-11)

#### 6.1 Parser Core
- [ ] Implement `TextParser` public interface
- [ ] Implement `TextParserImpl` internal implementation
- [ ] Implement `LineSource` interface
- [ ] Implement `RegionHandler` interface

#### 6.2 Parsing Logic
- [ ] Implement scheme matching algorithm
- [ ] Handle block start/end matching
- [ ] Process keyword lists
- [ ] Handle scheme inheritance
- [ ] Support virtual entries

#### 6.3 Parse Caching
- [ ] Implement `ParseCache` structure
- [ ] Cache state per line
- [ ] Implement cache invalidation
- [ ] Support three parse modes:
  - `CacheOff` - No caching
  - `CacheRead` - Read-only cache
  - `CacheUpdate` - Update cache

#### 6.4 Performance Features
- [ ] Use `Span<char>` for line processing
- [ ] Implement incremental parsing
- [ ] Profile and optimize parse loop
- [ ] Add parse interruption support (`BreakParse()`)

**Deliverables**:
- Complete text parser implementation
- Cache system
- Parser state management

### Phase 7: Parser Factory (Week 12)

#### 7.1 Factory Implementation
- [ ] Implement `ParserFactory` class
- [ ] Load catalog.xml
- [ ] Create `HrcLibrary` instances
- [ ] Create `TextParser` instances
- [ ] Create mapper instances
- [ ] Handle configuration paths

#### 7.2 Factory Features
- [ ] Enumerate available HRD schemes
- [ ] FileType enumeration
- [ ] Default catalog search paths
- [ ] Custom catalog loading

**Deliverables**:
- Complete ParserFactory
- End-to-end initialization

### Phase 8: Integration & Testing (Weeks 13-14)

#### 8.1 Integration Tests
- [ ] Test complete parsing pipeline
- [ ] Test with real syntax files (C, C++, Java, Python, etc.)
- [ ] Validate output against C++ version
- [ ] Test edge cases and error conditions

#### 8.2 Sample Applications
- [ ] Create console sample application
  - Load syntax file
  - Highlight code file
  - Output to console with colors
- [ ] Create performance test harness
- [ ] Create syntax validation tool

#### 8.3 Performance Testing
- [ ] Benchmark against C++ version
- [ ] Profile memory usage
- [ ] Identify bottlenecks
- [ ] Optimize critical paths
- [ ] Document performance characteristics

**Deliverables**:
- Comprehensive integration tests
- Sample applications
- Performance benchmarks
- Optimization documentation

### Phase 9: Documentation & Polish (Week 15)

#### 9.1 API Documentation
- [ ] XML documentation comments on public APIs
- [ ] Generate API reference documentation
- [ ] Create getting-started guide
- [ ] Create HRC/HRD syntax reference

#### 9.2 User Documentation
- [ ] README.md with usage examples
- [ ] Migration guide from C++ version
- [ ] Configuration guide
- [ ] Troubleshooting guide

#### 9.3 Code Quality
- [ ] Address all analyzer warnings
- [ ] Code review and refactoring
- [ ] Ensure consistent coding style
- [ ] Final test coverage review (target >85%)

**Deliverables**:
- Complete documentation
- Polished codebase
- Release-ready package

### Phase 10: Packaging & Release (Week 16)

#### 10.1 NuGet Package
- [ ] Create NuGet package specification
- [ ] Include data files (HRC/HRD)
- [ ] Package versioning strategy
- [ ] Create package README

#### 10.2 Release Preparation
- [ ] Tag version in Git
- [ ] Create release notes
- [ ] Publish NuGet package
- [ ] Update documentation links

**Deliverables**:
- Published NuGet package
- GitHub release
- Release documentation

## Testing Strategy

### Unit Testing

**Coverage Target**: >90% for core components

**Key Areas**:
1. **Regular Expression Engine**
   - Every operator type
   - Every metacharacter
   - Character class operations
   - Backreferences
   - Edge cases (empty matches, nested groups, etc.)

2. **Parser Components**
   - Scheme matching
   - Region detection
   - Cache operations
   - Error handling

3. **XML Processing**
   - Catalog parsing
   - HRC parsing
   - HRD parsing
   - Malformed input handling

4. **I/O Layer**
   - File access
   - ZIP archives
   - Error conditions

### Integration Testing

**Test Scenarios**:
1. Parse complete files with various syntax types
2. Validate region positions and types
3. Test with real-world HRC/HRD files from the data/ directory
4. Verify cache consistency across incremental parses
5. Test thread safety (if applicable)

### Performance Testing

**Benchmarks**:
1. **Regex Engine**
   - Pattern compilation time
   - Match execution time
   - Memory usage
   - Compare with System.Text.RegularExpressions

2. **Parser**
   - Lines per second
   - Memory per parsed line
   - Cache overhead
   - Incremental parse performance

3. **End-to-End**
   - File load time
   - Full file parse time
   - Memory footprint
   - Compare with C++ version

**Tools**:
- BenchmarkDotNet for microbenchmarks
- Custom test harness for large file testing
- Memory profiler (dotMemory or PerfView)

### Compatibility Testing

**Validation**:
1. Load all HRC files from data/ directory without errors
2. Verify parsed output matches C++ version for sample files
3. Test with edge-case syntax constructs
4. Ensure backward compatibility with existing syntax files

## Performance Optimization Guidelines


### General Principles
1. **Measure First**: Always profile before optimizing
2. **Hot Path Focus**: Optimize the regex matcher and parse loop first
3. **Memory Efficiency**: Minimize allocations in parse loop

### Specific Techniques

#### String Processing
```csharp
// Prefer Span<char> over string for parsing
public bool Match(ReadOnlySpan<char> input) { }

// Use stackalloc for small buffers
Span<int> positions = stackalloc int[16];

// Avoid string.Substring in loops
ReadOnlySpan<char> slice = input.Slice(start, length);
```

#### Memory Management
```csharp
// Use ArrayPool for temporary allocations
var buffer = ArrayPool<char>.Shared.Rent(1024);
try {
    // Use buffer
} finally {
    ArrayPool<char>.Shared.Return(buffer);
}

// Cache compiled regexes
private static readonly ColorerRegex CachedRegex =
    new ColorerRegex(pattern, RegexOptions.Compiled);
```

#### Regex Optimization
```csharp
// First-character optimization
if (pattern.StartsWith(literal)) {
    // Fast path for literal prefix
}

// Avoid backtracking where possible
// Use possessive quantifiers when appropriate
```

#### Parser Optimization
```csharp
// Cache scheme lookups
private readonly Dictionary<string, Scheme> _schemeCache;

// Reuse match objects
private readonly SMatches _matches = new SMatches();

// Minimize virtual calls in hot paths
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private bool QuickCheck(char c) { }
```

## Risk Mitigation

### High-Risk Areas

#### 1. Regular Expression Engine Complexity
**Risk**: Custom regex engine may have bugs or performance issues
**Mitigation**:
- Extensive unit testing
- Fuzz testing with generated patterns
- Compare output with C++ version
- Performance benchmarks
- Incremental development with continuous testing

#### 2. Backreference Compatibility
**Risk**: Cross-pattern backreferences are complex and error-prone
**Mitigation**:
- Test with real HRC files extensively
- Document limitations if any
- Create regression test suite

#### 3. Performance Regression
**Risk**: .NET version may be slower than C++
**Mitigation**:
- Performance testing from Phase 3 onward
- Profile early and often
- Use Span<T> and modern .NET features
- Consider unsafe code for critical sections if needed

#### 4. Unicode Handling Differences
**Risk**: Subtle differences between ICU and .NET Unicode handling
**Mitigation**:
- Test with diverse Unicode input
- Document any differences
- Use .NET's built-in Unicode category support

## Success Criteria

### Functional Requirements
- [ ] Load and parse all standard HRC files without errors
- [ ] Load and parse all standard HRD files without errors
- [ ] Parse sample code files (C, C++, Java, Python, XML, etc.)
- [ ] Produce correct region boundaries
- [ ] Apply color schemes correctly
- [ ] Support incremental parsing with caching

### Performance Requirements
- [ ] Parse speed within 50% of C++ version
- [ ] Memory usage comparable to C++ version
- [ ] Startup time <1 second for typical catalog

### Quality Requirements
- [ ] >85% code coverage
- [ ] Zero critical analyzer warnings
- [ ] All public APIs documented
- [ ] Comprehensive test suite passing

### Usability Requirements
- [ ] Clear API design
- [ ] Good error messages
- [ ] Sample applications demonstrating usage
- [ ] Complete documentation

## Timeline Summary

| Phase | Duration | Description |
|-------|----------|-------------|
| 1 | 2 weeks | Foundation & Infrastructure |
| 2 | 1 week | XML Processing |
| 3 | 3 weeks | Regular Expression Engine |
| 4 | 2 weeks | HRC Processing |
| 5 | 1 week | HRD Processing |
| 6 | 2 weeks | Text Parser |
| 7 | 1 week | Parser Factory |
| 8 | 2 weeks | Integration & Testing |
| 9 | 1 week | Documentation & Polish |
| 10 | 1 week | Packaging & Release |
| **Total** | **16 weeks** | **Full implementation** |

## Future Enhancements (Post-V1)

1. **Editor Integration**
   - Visual Studio extension
   - VS Code extension
   - WPF/Avalonia syntax highlighting controls

2. **Performance Improvements**
   - JIT compilation of regex patterns
   - Multi-threaded parsing for large files
   - SIMD optimizations for character matching

3. **Extended Features**
   - Syntax tree API
   - Code folding support
   - Symbol navigation
   - Semantic highlighting

4. **Tool Ecosystem**
   - HRC/HRD editor/validator
   - Syntax theme converter
   - Online syntax playground

## Conclusion

This plan provides a comprehensive roadmap for porting Colorer-library to .NET. The phased approach allows for incremental progress with regular validation points. The focus on testing, performance, and compatibility ensures a high-quality result that can serve as a reliable syntax highlighting solution for .NET applications.

The most critical component is the regular expression engine (Phase 3), which should receive extra attention and resources. Success in this phase is essential for the overall project success.
