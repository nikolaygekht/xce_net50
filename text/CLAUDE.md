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

## Binding Design Decisions (D1–D6)

These are the canonical source for *why* the code looks the way it does. Tests pin them;
code reviews enforce them. Don't "fix" one without changing the decision first.

| # | Decision | Rationale |
|---|---|---|
| **D1** | One `ITextBufferCallback Owner` per buffer, not a multi-subscriber collection. `null` means no fan-out. | Every callback consumer needs state that lives on the parent editor anyway (path, view, theme, syntax mode), so the owner has to re-solve fan-out regardless. Dropping the collection removes concurrent add/remove thread-safety surface, ordering ambiguity, and duplicate-add semantics. |
| **D2** | Every public edit **always** pushes an undo entry and **always** fires its callback — even when nothing changed (`NoOpUndoAction`, `length = 0`). Empty transactions push too. | Uniformity beats optimization. One stack entry + one callback for a rare empty edit costs less than "is this a no-op?" branching scattered through every edit path. Callers never need to detect no-op cases. |
| **D3** | The buffer is conceptually infinite empty past real content. Past-end **inserts** auto-extend (tracked, removed on undo); past-end **deletes** are uniform no-ops. Partially-overlapping deletes clamp to real content. | Keeps the "is this index valid?" check in *one* place — the buffer — instead of in every caller. Editor code can treat positions past content as legal without guarding. |
| **D4** | Negative line/column/length arguments throw `ArgumentOutOfRangeException`. | Negatives have no natural "endless" interpretation; they're caller bugs and worth flagging loudly. |
| **D5** | File I/O preserves final-newline state byte-for-byte via the trailing-empty-line convention. No implicit normalization on read or write. | N trailing newlines on disk ↔ N trailing empty lines in the buffer. Round-trips must not silently rewrite files. |
| **D6** | `Cursor`, `Block`, and `Markers` are first-class `TextBuffer` members. There is **no** `IUndoState` plug-in surface and no public way to register a custom `IUndoAction`. | The buffer owns the state it must restore, so undo/redo can never lose it to a lossy callback stream. Externally-supplied undo-tracked state is explicitly out of scope. |

### Threading contract

`TextBuffer` guards its own internals (`mLines`, undo/redo stacks, transaction stack) with a
private reentrant `Monitor`; locks sit on the public boundary and the `*Internal` methods are
lock-free because their callers already hold it.

`Cursor`, `Block`, and `Markers` are **live mutable objects, deliberately not synchronized**.
The contract is **get / use / forget** — read a position, act on it, drop the reference. Do not
cache them across edits or hand them to another thread. In editor use, state mutation and text
mutation happen on the same thread (the UI thread), so no concurrent modification can occur and
locking them would be pure overhead. Multi-threaded mutation of buffer-owned state is *not*
supported and is out of scope.

`internal int LinesCountNoLock` exists so undo actions carry no reentrancy assumption — see its
XML doc. It asserts lock ownership in debug builds.

**Replay atomicity** (from the 2026-05-08 audit): during `Undo`/`Redo` the buffer suppresses
Owner fan-out and queues events, runs the inner action against its own trackers, restores the
snapshot, and only then flushes queued events inside an `OnReplayBegin` / `OnReplayEnd` bracket.
An Owner that throws cannot leave the buffer half-unwound.

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
- Four edit action types:
  - `InsertLineUndoAction` - tracks line insertion + auto-added lines
  - `DeleteLineUndoAction` - stores deleted line content
  - `InsertSubstringUndoAction` - tracks substring insertion + auto-added lines/spaces
  - `DeleteSubstringUndoAction` - stores deleted substring content
- Plus three infrastructure types (all `internal`):
  - `NoOpUndoAction` - singleton placeholder for empty / past-end edits (D2/D3)
  - `BufferStateSnapshot` - captures cursor caret/anchor, block coords/type, marker triples
  - `StateSnapshotUndoAction` - wraps every top-level edit/transaction so undo and redo
    restore full editor state, not just text
- `UndoCorruptedException` thrown and both stacks cleared if an action throws mid-undo/redo

#### Transaction Support
- `UndoTransaction` groups multiple actions
- Nested transactions supported (only the outermost commit produces a snapshot-wrapped entry)
- `BeginUndoTransaction()` returns `IDisposable`
- Pattern: `using (buffer.BeginUndoTransaction()) { ... }`
- `CommitTransaction` uses peek-then-pop so a mismatch can't corrupt the stack
- `Undo()` / `Redo()` throw `InvalidOperationException` while a transaction is open
- Double-dispose is harmless (commits exactly once)

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
Stack<TransactionFrame> mTransactionStack;  // transaction + pre-edit snapshot

// Aggregated editor state (D6) — buffer-owned, driven in-line, snapshot-restored
public TextCursor Cursor { get; }
public TextBufferBlock Block { get; }
public TextMarkerCollection Markers { get; }
public ITextBufferCallback Owner { get; set; }   // single sink (D1); null = no fan-out
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
- **Scintilla.CellBuffer.Test** - Gap buffer tests (24 total)
  - SimpleList tests
  - SplitList tests

- **Gehtsoft.Xce.TextBuffer.Test** - TextBuffer tests (305 total)
  - Basic operations, undo/redo, transactions, transaction lifecycle
  - Block selections, markers, edge semantics, state-undo scenarios
  - Cursor/selection, replay safety, uniform no-op, Owner contract, span-read API
  - File I/O, stress tests, thread safety
  - See `CURRENT_STATUS.md` for the full per-class breakdown (329 total across both projects)

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

### Custom Undo Actions — not an extension point (D6)
`IUndoAction` is public, but `RegisterUndoAction` is **private** and every concrete action type
(`NoOpUndoAction`, `StateSnapshotUndoAction`, `BufferStateSnapshot`) is `internal`. There is no
public way to inject a custom action, by design — see D6. Group multi-step operations with
`BeginUndoTransaction()` instead; that is the supported composition mechanism.

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
