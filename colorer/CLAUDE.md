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

#### 4. **Parser Factory**
Located in: `native/src/colorer/ParserFactory.h`

**Responsibilities**:
- Loads `catalog.xml` to discover HRC and HRD files
- Creates and manages HrcLibrary instances
- Creates TextParser instances
- Creates RegionMapper instances (StyledHRDMapper, TextHRDMapper)

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

#### 6. **HRC Library**
Located in: `native/src/colorer/HrcLibrary.h`

**Responsibilities**:
- Manages all loaded HRC syntax definitions
- Provides access to FileType definitions
- Manages scheme inheritance and resolution

#### 7. **Region System**
Located in: `native/src/colorer/Region.h`, `handlers/`

**Purpose**:
- Defines region types (syntax elements)
- Maps regions to visual representations
- Handlers process region information for output

#### 8. **String Handling**
Located in: `native/src/colorer/strings/`

**Two Implementations**:
- `icu/` - ICU library-based (modern, default)
- `legacy/` - Legacy implementation

**Key Classes**:
- `UnicodeString` - 16-bit Unicode string container
- `Character` - Unicode character utilities
- `CharacterClass` - Character classification

#### 9. **XML Processing**
Located in: `native/src/colorer/xml/`

**Implementation**:
- Uses libxml2 for parsing HRC/HRD files
- `XmlReader` interface for XML access
- `XmlInputSource` for file/stream input

#### 10. **I/O System**
Located in: `native/src/colorer/io/`

**Components**:
- `InputSource` - Abstract input source
- `FileInputSource` - File-based input
- ZIP support for compressed HRC/HRD catalogs

## Dependencies

### C++ Version Dependencies
- **ICU** (International Components for Unicode) - string handling
- **libxml2** - XML parsing
- **minizip** - ZIP archive support
- **zlib** - compression

### Build System
- CMake 3.10+
- C++14 or higher (gcc 7+, clang 7+, MSVC 2019+)

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
