# Text Buffer Library - Project Overview

## Project Description

A high-performance text buffer library for .NET text editors, implemented in C#. This library provides efficient text manipulation capabilities using a gap buffer data structure, complete with undo/redo support and position marker tracking.

## Architecture

### Core Components

#### 1. **Gehtsoft.Xce.TextBuffer** (Main Library)
The primary text buffer implementation with:
- `TextBuffer` - Main text buffer class with line-based text storage
- `PositionMarker` - Tracks positions that auto-update during edits
- `TextBufferStatus` - Stores cursor position, block selection state
- `BlockMode` - Enum for selection modes (None, Line, Box, Stream)
- `EolMode` - Enum for line ending types (CR, LF, CRLF)

#### 2. **Scintilla.CellBuffer** (Data Structures)
Efficient data structures for text storage:
- `SplitList<T>` - Gap buffer implementation for O(1) insertions
- `SimpleList<T>` - Underlying dynamic array with move operations

#### 3. **Undo System**
Comprehensive undo/redo framework:
- `UndoActionCollection` - Stack-based undo/redo management
- `UndoAction` - Base class for undoable operations
- `UndoTransaction` - Groups multiple operations into single undo unit
- `UndoActionInsertLine` - Undo for line insertions
- `UndoActionRemoveLines` - Undo for line deletions

### Key Design Patterns

1. **Gap Buffer Pattern**: `SplitList<T>` uses a gap buffer for efficient text editing
2. **Command Pattern**: Undo/redo system uses command objects
3. **Observer Pattern**: Position markers automatically adjust during edits
4. **Transaction Pattern**: Multiple operations can be grouped into transactions

## Data Structures

### TextBuffer Storage
```
TextBuffer
  └─ SplitList<SplitList<char>>
       └─ Each line is a SplitList<char>
            └─ Gap buffer for efficient character insertion/deletion
```

### Gap Buffer (SplitList)
```
[Part1][Gap][Part2]
```
- Gap positioned at edit location
- Insertions/deletions at gap position are O(1)
- Gap moves when editing at different position

## API Overview

### Text Manipulation
```csharp
// Line operations
void AppendLine(string text)
void InsertLine(int line, string text)
void RemoveLine(int line)
void RemoveLines(int line, int count)

// Character operations
void InsertSubstring(int line, int position, string text)
void InsertCharacter(int line, int position, char character)
void RemoveSubstring(int line, int position, int length)

// Access
string GetLine(int line)
void GetLine(int line, out char[] target)
string GetSubstring(int line, int column, int length)
int LinesCount { get; }
```

### Undo/Redo
```csharp
UndoActionCollection UndoCollection { get; }
UndoActionCollection RedoCollection { get; }

// Transactions
using (var transaction = buffer.UndoCollection.BeginTransaction())
{
    // Multiple operations undone as single unit
}
```

### Position Tracking
```csharp
TextBufferStatus Status { get; set; }
  - CursorPosition
  - BlockStart/BlockEnd
  - BlockMode

IReadOnlyList<PositionMarker> SavedPositions // 10 saved positions
```

## Project Structure

```
text/
├── Gehtsoft.Xce.TextBuffer/          # Main library
│   ├── TextBuffer.cs                  # Core text buffer
│   ├── PositionMarker.cs              # Position tracking
│   ├── TextBufferStatus.cs            # Editor state
│   ├── BlockMode.cs / EolMode.cs      # Enums
│   └── Undo/                          # Undo system
│       ├── UndoAction.cs
│       ├── UndoActionCollection.cs
│       ├── UndoTransaction.cs
│       └── UndoAction*.cs             # Specific undo actions
│
├── Scintilla.CellBuffer/              # Data structures
│   ├── SplitList.cs                   # Gap buffer
│   └── SimpleList.cs                  # Dynamic array
│
├── Gehtsoft.Xce.TextBuffer.Test/      # Unit tests
│   ├── TextBufferCore.cs              # Core functionality tests
│   ├── Undo.cs                        # Undo/redo tests
│   ├── TextBufferMarkers.cs           # Marker tracking tests
│   └── TextBufferStatus.cs            # Status tests
│
└── Scintilla.CellBuffer.Test/         # Data structure tests
    ├── SplitListTest.cs
    └── SimpleListTest.cs
```

## Technology Stack

- **Language**: C# 9+
- **Target Framework**: .NET 8.0
- **Testing**: xUnit, FluentAssertions, Moq
- **Build**: .NET SDK

## Building

```bash
dotnet build Gehtsoft.Xce.Text.sln
dotnet test Gehtsoft.Xce.Text.sln
```

## Thread Safety

The TextBuffer includes thread-safety mechanisms:
- `SyncRoot` object for locking
- All modification methods use `lock (SyncRoot)`
- Safe for concurrent read/write operations

## Performance Characteristics

### Time Complexity
- Insert at cursor: O(1) amortized
- Delete at cursor: O(1) amortized
- Insert/delete at arbitrary position: O(n) for gap movement
- Get line: O(n) where n = line length
- Line count: O(1)

### Space Complexity
- O(n) where n = total characters
- Each line has 16-character gap buffer overhead initially
- Gaps grow dynamically when needed

## Known Limitations

See [PLAN.md](PLAN.md) for detailed analysis, but key limitations:

1. **No undo for character-level edits** - Only line operations are undoable
2. **No file I/O** - No methods to load/save files
3. **No encoding support** - No UTF-8/UTF-16 handling
4. **No EOL handling** - EolMode defined but unused
5. **No range operations** - Cannot get/set multi-line text ranges
6. **No search/replace** - No built-in text search functionality

## Usage Example

```csharp
using var buffer = new TextBuffer();

// Add content
buffer.AppendLine("Hello, World!");
buffer.AppendLine("Line 2");
buffer.InsertLine(1, "Inserted line");

// Edit text
buffer.InsertSubstring(0, 5, " there");  // "Hello there, World!"
buffer.RemoveSubstring(0, 0, 6);         // "there, World!"

// Undo/redo
buffer.UndoCollection.Pop().Undo();      // Restore removed text

// Transactions
using (var tx = buffer.UndoCollection.BeginTransaction())
{
    buffer.AppendLine("Line A");
    buffer.AppendLine("Line B");
    // Both lines undone together
}

// Position markers
buffer.SavedPositions[0].Set(2, 5);      // Save position
buffer.InsertLine(1, "New line");
// Marker automatically adjusted to (3, 5)

// Block selection
buffer.Status.BlockMode = BlockMode.Stream;
buffer.Status.BlockStart.Set(0, 0);
buffer.Status.BlockEnd.Set(2, 10);
```

## Future Enhancements

See [PLAN.md](PLAN.md) for prioritized list of improvements.

## License

Part of the XCE text editor project.
