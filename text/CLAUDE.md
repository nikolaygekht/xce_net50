# TextBuffer Library - Project Overview

## Project Description

This is a high-performance text buffer library for text editor applications, built in C# with .NET 8. The library provides efficient text manipulation with gap buffer data structures, full undo/redo support, and automatic position tracking for markers and selections.

## Architecture

### Core Components

1. **Scintilla.CellBuffer** - Low-level gap buffer implementation
   - `SimpleList<T>` - Dynamic array with efficient operations
   - `SplitList<T>` - Gap buffer for efficient sequential insertions/deletions
   - Supports both indexed access and span-based operations

2. **Gehtsoft.Xce.TextBuffer** - High-level text buffer with editing features
   - `TextBuffer` - Main text buffer class managing lines
   - Undo/Redo system with transaction support
   - Callback system for change notifications
   - Block selections (Line, Box, Stream)
   - Position markers with automatic adjustment
   - File I/O with encoding and EOL detection

## Key Design Principles

### 1. Performance
- Gap buffers for O(1) insertions at cursor position
- Span<T> and ReadOnlySpan<T> for zero-allocation operations
- Aggressive inlining for frequently-called methods
- Stack allocation (stackalloc) for temporary buffers

### 2. Auto-Extension
- Inserting beyond buffer end automatically adds empty lines
- Inserting beyond line end automatically adds spaces
- All auto-extended content is tracked in undo/redo

### 3. DRY Principle
- String methods delegate to Span methods
- Single implementation point for each operation
- Internal methods with suppressUndo parameter

### 4. Callback System (single Owner sink — D1)
The buffer exposes one `ITextBufferCallback Owner` (nullable). The owner is the parent
editor object; it is responsible for fan-out to UI repaint, syntax highlighting, and
any other derived listeners. There is no multi-subscriber registry inside `TextBuffer`.
All buffer modifications fire callbacks through `Owner` (if non-null):
- `OnLinesInserted(lineIndex, count)`
- `OnLinesDeleted(lineIndex, count)`
- `OnSubstringInserted(lineIndex, columnIndex, length)`
- `OnSubstringDeleted(lineIndex, columnIndex, length)`

**Undo/Redo replay safety**: during `Undo()`/`Redo()` the buffer suppresses Owner
fan-out, runs the inner action against its own internal trackers, and only then
flushes the queued events to Owner inside an `OnReplayBegin` / `OnReplayEnd` bracket
(both default-implemented on `ITextBufferCallback`, so existing implementers don't
need to opt in). An Owner that throws during the flushed events cannot leave the
buffer in a half-unwound state; `OnReplayEnd` is delivered in a finally so the
Owner can reliably exit any "bulk update" mode it entered on `OnReplayBegin`.

**Uniform edit semantics (D2)**: every public edit method always pushes an undo entry
and always invokes the corresponding `Owner` callback — even when nothing actually
changed (length-0 callback, `NoOpUndoAction` entry). Callers don't need to detect
no-op cases.

**Endless past content (D3)**: the buffer is conceptually infinite empty past real
content. `DeleteLine`/`DeleteSubstring` past content are uniform no-ops; `InsertLine`
and `InsertSubstring` past content auto-extend (tracked, removed on undo).

**Negative indices throw (D4)**: any negative line/column/length argument throws
`ArgumentOutOfRangeException`.

### 5. Undo/Redo System

#### Command Pattern
- `IUndoAction` interface with `Undo()` and `Redo()` methods
- Four action types:
  - `InsertLineUndoAction` - tracks line insertion + auto-added lines
  - `DeleteLineUndoAction` - stores deleted line content
  - `InsertSubstringUndoAction` - tracks substring insertion + auto-added lines/spaces
  - `DeleteSubstringUndoAction` - stores deleted substring content

#### Transaction Support
- `UndoTransaction` groups multiple actions
- Nested transactions supported
- `BeginUndoTransaction()` returns `IDisposable`
- Pattern: `using (buffer.BeginUndoTransaction()) { ... }`

#### Auto-Extension Tracking
- Auto-added empty lines are tracked and removed on undo
- Auto-added spaces are tracked and removed on undo
- Redo automatically re-extends as needed

## Data Structures

### TextBuffer
```csharp
SplitList<SplitList<char>> mLines;  // Each line is a gap buffer
Stack<IUndoAction> mUndoActions;
Stack<IUndoAction> mRedoActions;
Stack<UndoTransaction> mTransactionStack;
```

### Block Selection Types

1. **Line Block** - Full lines from first to last
   - Only line coordinates matter
   - Adjusts on line insertions/deletions

2. **Box Block** - Rectangular selection
   - Fixed column coordinates (don't adjust for substring operations)
   - Adjusts on line insertions/deletions
   - Use case: columnar editing

3. **Stream Block** - Standard text selection
   - From first position to last position
   - Adjusts for both line and substring operations
   - Use case: normal copy/paste

### Markers
- Simple position holders with id, line, and column
- Column-aware: adjust on both line and substring operations using the same rules as `TextCursor`
  - line insert/delete: shift / snap-to-deletion-start (column reset to 0 when the marker's line is deleted)
  - substring insert at `columnIndex <= marker.Column` on the marker's line: column shifts right by `length`
  - substring delete entirely before the marker on its line: column shifts left
  - substring delete overlapping the marker on its line: column clamps to the deletion start
- Exposed as buffer-owned `buffer.Markers` (first-class member, driven by the buffer directly)

## File I/O

### TextBufferMetadata
- `FileName` - file path
- `Encoding` - text encoding (UTF-8, UTF-16, UTF-32, etc.)
- `SkipBom` - whether to skip BOM when writing
- `EolMode` - end-of-line mode (CrLf, Cr, Lf)

### TextBufferReader
- Automatic BOM detection
- Automatic encoding detection (from BOM)
- Automatic EOL mode detection (counts occurrences)
- Returns tuple of `(TextBuffer, TextBufferMetadata)`

### TextBufferWriter
- Respects all metadata settings
- Can write with or without BOM
- Uses correct EOL sequence for the mode
- Supports UTF-8, UTF-16 LE/BE, UTF-32 LE/BE

## Method Naming Conventions

### Public API
- `InsertLine(lineIndex, text)` - public, creates undo action
- `DeleteLine(lineIndex)` - public, creates undo action
- `InsertSubstring(lineIndex, columnIndex, text)` - public, creates undo action
- `DeleteSubstring(lineIndex, columnIndex, length)` - public, creates undo action

### Internal Implementation
- `InsertLineInternal(lineIndex, text, suppressUndo)` - all logic
- `DeleteLineInternal(lineIndex, suppressUndo)` - all logic
- `InsertSubstringInternal(lineIndex, columnIndex, text, suppressUndo)` - all logic
- `DeleteSubstringInternal(lineIndex, columnIndex, length, suppressUndo)` - all logic

### Helper Methods
- `EnsureLineExists(lineIndex)` - adds empty lines, fires callbacks
- `EnsureColumnExists(lineIndex, columnIndex)` - adds spaces, fires callbacks

## Testing Strategy

### Test Organization
- **Scintilla.CellBuffer.Test** - Gap buffer tests
  - SimpleList tests
  - SplitList tests

- **Gehtsoft.Xce.TextBuffer.Test** - TextBuffer tests (227 total)
  - Basic operations, undo/redo, transactions, transaction lifecycle
  - Block selections, markers, edge semantics, state-undo scenarios
  - File I/O, stress tests, thread safety
  - See `CURRENT_STATUS.md` for the full per-class breakdown

### Test Coverage
- All operations tested
- Boundary conditions tested
- Integration tests with callbacks
- Round-trip I/O tests
- Undo/redo with auto-extension

## Common Usage Patterns

### Basic Editing
```csharp
var buffer = new TextBuffer(new[] { "line1", "line2" });
buffer.InsertLine(1, "new line");
buffer.InsertSubstring(0, 5, " inserted");
buffer.DeleteSubstring(0, 0, 5);
buffer.Undo();
buffer.Redo();
```

### Transactions
```csharp
using (buffer.BeginUndoTransaction())
{
    buffer.InsertLine(0, "header");
    buffer.InsertLine(buffer.LinesCount, "footer");
    // Both operations undo/redo as one
}
```

### Block Selection
```csharp
// Use the buffer-owned primary block; the buffer drives its adjustments directly.
buffer.Block.SetStream(0, 5, 10, 20);
// Block automatically adjusts as buffer changes; round-trips through undo/redo.
```

### Markers
```csharp
buffer.Markers.Add(new TextMarker("bookmark1", 5, 10));
// Markers automatically adjust as buffer changes; round-trip through undo/redo.
```

### Owner callback (UI repaint, syntax highlighting, etc.)
```csharp
class MyEditor : ITextBufferCallback
{
    public void OnLinesInserted(int line, int count) { /* repaint */ }
    public void OnLinesDeleted(int line, int count) { /* repaint */ }
    public void OnSubstringInserted(int line, int col, int len) { /* repaint */ }
    public void OnSubstringDeleted(int line, int col, int len) { /* repaint */ }
}

buffer.Owner = new MyEditor();
// Owner is the single sink. Fan-out to other listeners is the owner's job.
```

### File I/O
```csharp
// Reading
var (buffer, metadata) = TextBufferReader.Read("file.txt");

// Writing
TextBufferWriter.Write(buffer, metadata);

// Custom settings
var metadata = new TextBufferMetadata(
    "output.txt",
    Encoding.UTF8,
    skipBom: true,
    EolMode.Lf
);
TextBufferWriter.Write(buffer, metadata);
```

## Performance Considerations

### When to Use Transactions
- Multiple related operations
- Complex editing that should undo as one unit
- Better than individual undo actions

### Memory Efficiency
- Use Span methods when possible
- Avoid string allocations in hot paths
- Gap buffers reuse memory efficiently

### Callback Performance
- Callbacks are invoked synchronously
- Keep callback implementations fast
- Consider batching updates in UI

## Extension Points

### Custom Callbacks
Implement `ITextBufferCallback` for:
- Syntax highlighting updates
- Line number updates
- Custom position tracking
- External data structure synchronization

### Custom Undo Actions
Implement `IUndoAction` for:
- Complex multi-step operations
- Operations involving external resources
- Custom state management

## Future Considerations

### Potential Enhancements
- Async file I/O
- Partial file loading for large files
- Text search/replace infrastructure
- Multi-cursor support
- Collaborative editing (operational transform)

### Not Currently Supported
- Multi-byte character handling in columns (uses char offsets)
- Regex operations
- Syntax highlighting (delegate to callbacks)
- Line wrapping (display layer concern)
