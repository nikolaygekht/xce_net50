# TextBuffer Library - Current Status

**Last Updated**: 2026-05-08 (audit-resolution pass)
**Total Tests Passing**: 329 / 329 (100%) — 24 in Scintilla.CellBuffer.Test, 305 in Gehtsoft.Xce.TextBuffer.Test

## Implementation Status

### ✅ Completed Components

#### 1. Core Gap Buffer (Scintilla.CellBuffer)
- [x] `SimpleList<T>` - Dynamic array implementation
- [x] `SplitList<T>` - Gap buffer with efficient insertions
- [x] Span support for zero-allocation operations
- [x] 24 unit tests passing

#### 2. TextBuffer Core (Gehtsoft.Xce.TextBuffer)
- [x] Line-based text storage
- [x] Auto-extension (lines and columns with spaces)
- [x] Span-based API for performance
- [x] String API delegates to Span methods (DRY)
- [x] Thread-safety with lock-based synchronization
- [x] First-class aggregated editor state: `Cursor`, `Block`, `Markers` (no external registration required)

#### 3. Callback System (single Owner sink — D1)
- [x] `ITextBufferCallback` interface (UI repaint, syntax highlighting, derived listeners)
- [x] `TextBuffer.Owner` — single nullable callback sink set by the parent editor object
- [x] No multi-subscriber registry — fan-out (theme, syntax, observers) is the owner's responsibility
- [x] Four edit callback types:
  - `OnLinesInserted`
  - `OnLinesDeleted`
  - `OnSubstringInserted`
  - `OnSubstringDeleted`
- [x] **Replay-batch signals** (default-implemented, opt-in via override):
  - `OnReplayBegin` — fired right before queued events from a single `Undo`/`Redo` are flushed
  - `OnReplayEnd` — fired in a finally after the flush, even if a queued event throws
- [x] Buffer drives `Cursor`/`Block`/`Markers` directly — they no longer need to be registered as callbacks
- [x] **Uniform edit semantics (D2)**: every edit pushes an undo entry and fires a callback (length-0 for no-op cases)
- [x] **Replay safety**: during `Undo`/`Redo`, Owner fan-out is suppressed and queued; the inner action runs end-to-end against the buffer's internal trackers, the snapshot is restored, and only then are the queued events flushed to Owner inside the `OnReplayBegin` / `OnReplayEnd` bracket — an Owner that throws cannot leave the buffer half-unwound

#### 4. Undo/Redo System
- [x] `IUndoAction` interface
- [x] Four edit-action types:
  - `InsertLineUndoAction` (with auto-added line tracking)
  - `DeleteLineUndoAction` (with deleted content storage)
  - `InsertSubstringUndoAction` (with auto-added lines/spaces tracking)
  - `DeleteSubstringUndoAction` (with deleted content storage)
- [x] `NoOpUndoAction` — singleton placeholder for empty / past-end edits (D2/D3)
- [x] `BufferStateSnapshot` capturing cursor caret/anchor, block coords/type, and marker triples
- [x] `StateSnapshotUndoAction` wraps every top-level edit/transaction so undo and redo restore the full editor state (not just text)
- [x] `UndoCorruptedException` thrown + stacks cleared if an action throws mid-undo/redo
- [x] Stack-based undo/redo with `CanUndo` / `CanRedo`
- [x] Undo entry registered *before* the Owner callback fires, so an Owner exception leaves a buffer that's both consistent and rollback-able

#### 5. Transaction System
- [x] `UndoTransaction` composite action
- [x] `BeginUndoTransaction()` with `IDisposable` pattern
- [x] Nested transactions (only outermost commit produces a snapshot-wrapped entry)
- [x] `Undo`/`Redo` rejected while a transaction is open
- [x] **Empty transactions push uniformly** (D2) — their Undo/Redo are harmless no-ops
- [x] `CommitTransaction` uses peek-then-pop for safe lifecycle

#### 6. Block Selection System
- [x] `TextBufferBlockType` enum (None, Line, Box, Stream)
- [x] `TextBufferBlock` class with validation
- [x] `buffer.Block` is the buffer-owned primary block (defaults to `BlockType.None`)
- [x] Line block - adjusts only on line operations
- [x] Box block - columns stay fixed, only lines adjust
- [x] Stream block - adjusts for both line and substring operations
- [x] Block type preserved even when invalid
- [x] Full-encompass-delete + undo restores block coordinates exactly (via snapshot)

#### 7. Cursor
- [x] `TextCursor` first-class member at `buffer.Cursor`
- [x] Caret `(Line, Column)` + selection anchor `(AnchorLine, AnchorColumn)`
- [x] `MoveTo`, `SetSelection`, `Collapse`, `HasSelection`
- [x] Adjusts on line ops like markers; adjusts on substring ops like a stream block
- [x] Caret on a deleted line snaps to the deletion point with column 0; restored exactly on undo via snapshot

#### 8. Marker System
- [x] `TextMarker` (id, line, column)
- [x] `TextMarkerCollection` exposed as `buffer.Markers`
- [x] **Column-aware**: adjusts on both line and substring operations (mirrors `TextCursor`)
  - line insert/delete: shift / snap-to-deletion-start (column reset to 0 when the marker's line is fully deleted)
  - substring insert at `columnIndex <= marker.Column` on the marker's line: column shifts right by `length`
  - substring delete entirely before the marker on its line: column shifts left
  - substring delete overlapping the marker on its line: column clamps to the deletion start
- [x] Lost-column-on-line-delete restored on undo via snapshot
- [x] `FindById`, `RemoveById`, `Clear`

#### 9. File I/O System
- [x] `EolMode` enum (CrLf, Cr, Lf)
- [x] `TextBufferMetadata` class
- [x] `TextBufferReader` with automatic detection (BOM, encoding, EOL mode); explicit-encoding override supported
- [x] `TextBufferWriter` with full control (BOM on/off, EOL mode, multiple encodings)
- [x] **D5 — final-newline byte-for-byte preservation** via the trailing-empty-line convention (a file with N trailing newlines reads into N corresponding trailing empty lines)
- [x] Round-trip preservation (encoding, BOM, EOL mode, final-newline state)

## Test Summary

### Test Distribution (Gehtsoft.Xce.TextBuffer.Test)
| Test Class | Tests |
|---|---|
| TextBuffer_BasicOperations | 29 |
| TextBuffer_UndoRedo | 22 |
| TextBuffer_Transactions | 15 |
| TextBuffer_TransactionLifecycle | 7 |
| TextBuffer_TransactionIdempotence | 2 |
| TextBuffer_UndoAutoExtend | 7 |
| TextBuffer_OwnerContract | 6 |
| TextBuffer_StateUndoScenarios | 14 |
| TextBuffer_CursorSelection | 11 |
| TextBuffer_RedoStateRoundTrip | 4 |
| TextBuffer_ReplaySafety | 9 |
| TextBuffer_EdgeSemantics | 12 |
| TextBuffer_ReadApi | 13 |
| TextBuffer_UniformNoOp | 7 |
| TextBuffer_StressTests | 13 |
| TextBuffer_ThreadSafety | 2 |
| TextBufferBlock_Validation | 20 |
| TextBufferBlock_Adjustments | 33 |
| TextMarker_Tests | 36 |
| TextBufferIO_Tests | 43 |
| **Subtotal** | **305** |

Plus **24** tests in `Scintilla.CellBuffer.Test` → **329 total, 100% passing**.

(Multi-subscriber concurrent add/remove tests were retired in PR1 — that surface no longer exists.)

### Test Categories Covered
- Basic CRUD, boundary conditions, auto-extension behaviour
- Undo/Redo correctness, undo of auto-extended content
- **Transaction lifecycle** — peek-then-pop commit, undo/redo blocked while open, corruption handling
- **Transaction idempotence** — double-Dispose harmless, manual + using-exit harmless
- Nested transactions, transaction with auto-extension, empty transactions push uniformly
- Block validation and adjustment (Line / Box / Stream)
- Marker position tracking and collection ops
- **State-undo scenarios** — cursor / block / markers round-trip through edits and transactions
- **Cursor / selection** — reversed selections, anchor on deleted line, span-vs-adjacent insert, Collapse / HasSelection invariants
- **Redo state round-trip** — edit→undo→redo lands cursor+block+markers exactly at post-edit state
- **Edge semantics** — pinned boundary rules for block adjustments
- **Span-read API** — pinned non-throwing contract for `GetLine`/`GetSubstring` boundaries
- **Uniform no-op pinning** — empty/past-end edits push undo + length-0 callback (D2/D3)
- **Owner contract** — null Owner safe; reassignment routes correctly; Owner-throw → exception propagates with consistent buffer + undo entry on stack
- **Replay safety** — `OnReplayBegin`/`OnReplayEnd` bracket the queued events from a single Undo/Redo; the buffer is fully restored before any Owner method is called; an Owner that throws on a flushed event still gets `OnReplayEnd` (in finally) and leaves the buffer consistent; the audit's auto-extended-insert regression is pinned
- **Stress tests** — large-line / large-substring inserts and deletes (up to 10 MB), deep auto-extend (line index 100 000)
- **Thread safety** — concurrent edits with the single Owner attached
- **I/O policy** — encoding/EOL/BOM detection, round-trip; D5 final-newline byte-for-byte; explicit-encoding override; null-arg validation; unwritable target propagates `DirectoryNotFoundException`

## File Structure

```
text/
├── Scintilla.CellBuffer/                  # Gap buffer implementation
│   ├── SimpleList.cs
│   └── SplitList.cs
│
├── Scintilla.CellBuffer.Test/             # Gap buffer tests
│   ├── SimpleListTest.cs
│   └── SplitListTest.cs
│
├── Gehtsoft.Xce.TextBuffer/               # Main library
│   ├── TextBuffer.cs                      # Core (~889 lines) — owns Cursor/Block/Markers, single Owner sink
│   ├── TextCursor.cs                      # First-class caret + anchor (123 lines)
│   ├── TextBufferBlock.cs                 # Block selection (327 lines)
│   ├── TextBufferBlockType.cs
│   ├── TextMarker.cs
│   ├── TextMarkerCollection.cs            # 204 lines
│   ├── ITextBufferCallback.cs             # Single sink interface (no multi-subscriber registry)
│   │
│   ├── Undo/
│   │   ├── IUndoAction.cs
│   │   ├── InsertLineUndoAction.cs
│   │   ├── DeleteLineUndoAction.cs
│   │   ├── InsertSubstringUndoAction.cs
│   │   ├── DeleteSubstringUndoAction.cs
│   │   ├── UndoTransaction.cs
│   │   ├── BufferStateSnapshot.cs         # Cursor/Block/Marker snapshot
│   │   ├── StateSnapshotUndoAction.cs     # Wraps inner action; restores after Undo/Redo
│   │   ├── NoOpUndoAction.cs              # Placeholder entry for past-end / empty edits
│   │   └── UndoCorruptedException.cs
│   │
│   ├── EolMode.cs
│   ├── TextBufferMetadata.cs
│   ├── TextBufferReader.cs                # 206 lines
│   └── TextBufferWriter.cs                # 133 lines
│
└── Gehtsoft.Xce.TextBuffer.Test/          # See test table above
```

## Key Behaviours

### Aggregated editor state
`buffer.Cursor`, `buffer.Block`, and `buffer.Markers` are first-class buffer members. Edits update them in-line; undo/redo restore them exactly via `BufferStateSnapshot`. There is no external `IUndoState` extension hook — `ITextBufferCallback` remains the right surface for UI repaint and derived listeners only.

### Snapshot capture points
- Top-level public edits (outside any transaction): snapshot before, run, snapshot after, wrap in `StateSnapshotUndoAction`, push.
- `BeginUndoTransaction` (outermost): snapshot before, stored in the transaction frame; on outermost `Commit` snapshot after, wrap, push.
- Nested transactions and `*Internal` calls (`suppressUndo: true`) never snapshot.

### Auto-extension
- Inserting at line 5 when buffer has 2 lines → adds 3 empty lines (tracked, removed on undo).
- Inserting at column 10 when line has 2 chars → adds 8 spaces (tracked, removed on undo).
- A single `InsertSubstring` past line end fires multiple `OnSubstringInserted` callbacks (one for the auto-spaces, one for the actual text).

### Block adjustments
- **Line blocks**: adjust only for line operations.
- **Box blocks**: columns fixed, only lines adjust.
- **Stream blocks**: both lines and columns adjust.
- Invalid blocks keep their type (don't change to None).
- Full-encompass deletions are lossy in the forward direction but restored exactly on undo.

### Marker adjustments
- Column-aware: adjust on both line and substring operations using the same rules as the cursor.
- Substring insert at `columnIndex <= marker.Column` on the marker's line shifts the column right by `length`.
- Substring delete entirely before the marker on its line shifts it left; a delete overlapping the marker clamps the column to the deletion start.
- Markers on deleted lines move to deletion point with column 0; original column restored on undo via snapshot.

### File I/O
- Default encoding: UTF-8 without BOM.
- Default EOL: CRLF (Windows).
- Mixed EOL files: detects most common type.
- BOM detection: automatic for all UTF variants.
- **Final-newline byte-for-byte (D5)**: a file ending with N trailing newlines reads into a buffer with N corresponding trailing empty lines, and writes back with the same byte count. A file with no trailing newline round-trips with no trailing newline.
- Explicit-encoding override on `TextBufferReader.Read(fileName, encoding)` wins over auto-detection; BOM detection still runs against the override's preamble for skip semantics.

## Recent Changes

### 2026-05-08 — Audit-resolution pass: replay safety + column-aware markers
Closes both findings from the 2026-05-08 audit reevaluation (`AUDIT_REPORT.md`).
- **Replay safety (was High)**: during `Undo()`/`Redo()` the buffer suppresses Owner fan-out and queues every event. The inner action runs end-to-end against the buffer's own internal trackers (cursor, block, markers); the `BufferStateSnapshot` is restored on top; only then are queued events flushed to Owner inside an `OnReplayBegin` / `OnReplayEnd` bracket. New default-implemented methods on `ITextBufferCallback` — existing implementers don't need to opt in. An Owner that throws on a flushed event still gets `OnReplayEnd` (in finally) and the buffer is already in its fully-restored state — no half-unwound corruption. New `TextBuffer_ReplaySafety` test class (9 tests) including the audit's auto-extended-insert regression case.
- **Column-aware markers (was Medium)**: `TextMarkerCollection.OnSubstringInserted` / `OnSubstringDeleted` now mirror `TextCursor` semantics — insert at `columnIndex <= marker.Column` shifts right by `length`; delete entirely before the marker shifts left; delete overlapping the marker clamps to the deletion start. Coverage: 9 new substring tests in `TextMarker_Tests` plus 3 buffer-integration tests (insert-before-marker, delete-overlapping, undo/redo round-trip).
- **Auto-extension cascade with throwing Owner**: previously called out as the one remaining edge case where a partial buffer state could be left without a corresponding undo entry. The replay-safety mechanism (queue-then-flush) covers this for `Undo`/`Redo`; for forward edits, the existing `try/finally` around `WrapLastUndoActionWithSnapshot` keeps the undo entry on the stack so the caller can still roll back.

### 2026-05-08 — PR4: low-priority test gaps closed + small Owner-throw source fix
- **PR4.1 (transaction idempotence)** — 2 tests in `TextBuffer_TransactionIdempotence`: double-Dispose of a transaction scope is harmless and commits exactly once; manual-then-using exit is also harmless.
- **PR4.2 (Owner contract)** — 6 tests in `TextBuffer_OwnerContract`: `Owner = null` edits succeed (no NRE); reassignment routes correctly between old and new owners; setting Owner back to null detaches the previous one. Owner exceptions during a callback propagate but the buffer mutation is observable AND the undo entry is on the stack so the caller can roll the edit back.
- **Source fix**: `InsertLine`/`InsertSubstring` now register the undo entry *before* firing the Owner callback (matching `DeleteLine`/`DeleteSubstring`), and all four public edit methods wrap the inner call in `try/finally` so `WrapLastUndoActionWithSnapshot` runs even when Owner throws. This makes the "buffer consistent + undo entry on stack" guarantee actual, not just aspirational.

### 2026-05-08 — PR3: medium-priority test gaps closed
- **PR3.1 (redo state round-trip)** — 4 tests in `TextBuffer_RedoStateRoundTrip`: edit→undo→redo lands cursor/block/markers exactly at post-edit state; same through transactions; repeated undo↔redo cycles converge to identical state; "select-then-overtype" canonical flow round-trips.
- **PR3.2 (I/O policy pinning, D5)** — 14 tests added to `TextBufferIO_Tests`: final-newline byte-for-byte preservation (zero / one / two trailing newlines, both LF and CRLF) via the trailing-empty-line convention; explicit-encoding override identity preserved; reader/writer null-arg validation; unwritable target propagates `DirectoryNotFoundException`.
- **PR3.3 (large-insert / deep auto-extend stress)** — 3 tests added to `TextBuffer_StressTests`: 10 MB single-substring insert + undo + redo + content equality; insert at line 100,000 from empty buffer; combined deep line/column auto-extend.

### 2026-05-08 — PR2: high-priority test gaps closed
- **PR2.1 (cursor/selection)** — 11 tests in `TextBuffer_CursorSelection`: reversed selections, anchor-on-deleted-line, non-collapsed selection round-trip through undo and redo, `Collapse`/`HasSelection` invariants, selection-spanning-vs-adjacent-to insert.
- **PR2.2 (span-read API)** — 13 tests in `TextBuffer_ReadApi`: pinned non-throwing contract for `GetLine(Span)`, `GetSubstring(Span)`, and the `string` overload — truncation, exact fit, larger-target-untouched, zero-length, out-of-range, past-end column, negative arguments.
- **PR2.3 (uniform-no-op pinning)** — 7 tests in `TextBuffer_UniformNoOp`: empty inserts / past-end deletes / empty transactions push undo + fire length-0 callback; negative arguments throw without leaving partial entries. Uses a small `CountingSink` helper as `buffer.Owner`.

### 2026-05-08 — PR1: single Owner sink + uniform edit semantics (FIX_PLAN.md design D1–D4)
1. **D1 — Single-owner callback sink**
   - `TextBufferCallbackCollection` deleted; `TextBuffer.Owner` is the sole sink.
   - Tests probing concurrent multi-subscriber add/remove retired — surface no longer exists.
2. **D2 — Uniform edit semantics**
   - Every edit pushes an undo entry and fires a callback (length-0 for no-op cases).
   - New `NoOpUndoAction` is the placeholder for empty / past-end edits.
   - Empty transactions now push uniformly; their Undo/Redo are harmless no-ops.
3. **D3 — Endless buffer past real content**
   - `DeleteLine(N>=LinesCount)` and `DeleteSubstring(L,C,k)` past content are uniform no-ops.
   - `InsertLine` / `InsertSubstring` past real content still auto-extend (tracked, removed on undo).
4. **D4 — Negative indices throw**
   - All four edit methods throw `ArgumentOutOfRangeException` on negative L/C/k.
   - Pre-existing behaviour; tests confirmed in `TextBuffer_BasicOperations`.

### 2026 — aggregated state with undo round-trip (FIX_PLAN.md Phases 1–3)
1. **Phase 1 — transaction lifecycle hardening**
   - `CommitTransaction` peek-then-pop.
   - `Undo`/`Redo` rejected while a transaction is open.
   - `UndoCorruptedException` thrown and stacks cleared on action failure.
2. **Phase 2 — `BufferStateSnapshot` infrastructure**
   - New `BufferStateSnapshot` and `StateSnapshotUndoAction`.
   - Snapshot capture wired into top-level edits and outermost transaction commit.
3. **Phase 3 — aggregate Cursor/Block/Markers into `TextBuffer`**
   - New `TextCursor` first-class member.
   - `TextBufferBlock` and `TextMarkerCollection` exposed as buffer-owned `Block` / `Markers`.
   - Buffer drives their adjustment in-line; no `Callbacks.Add` registration required.
   - Bug fixes: marker survives line-delete + undo; block survives full-encompass-delete + undo; cursor returns home on undo and forward on redo; selection round-trips through transactions.
4. **Phase 4 (partial)**
   - Pinned boundary cases via `TextBuffer_EdgeSemantics` tests.
   - Stress and thread-safety test classes added.

### 2025-11-11 — earlier session
- Added auto-extension undo tracking and thread-safety locking.

## API Stability

### Stable
- TextBuffer basic operations
- Undo/Redo + transactions (with state snapshots)
- `Cursor`, `Block`, `Markers` aggregated state
- Callback system (for UI/derived listeners)
- File I/O system

### Internal (may change)
- Internal methods with `suppressUndo` parameter
- Helper methods (`EnsureLineExists`, `EnsureColumnExists`)
- Snapshot/restore plumbing
- Undo action internals

## Known Outstanding Work

- All four FIX_PLAN.md PRs (PR1–PR4) have landed, and the 2026-05-08 audit-resolution pass closed the two follow-up findings (replay safety, column-aware markers). No outstanding planned work on the buffer surface.
- Multi-cursor / multi-selection — explicitly out of scope.
- Multi-byte / grapheme handling — out of scope per `CLAUDE.md`.
- Async file I/O, search/replace, undo coalescing — future work.

## Build and Test

### Requirements
- .NET 8 SDK
- xUnit
- AwesomeAssertions
- Moq

### Running Tests
```bash
dotnet test
dotnet test --filter "FullyQualifiedName~TextBuffer_StateUndoScenarios"
```

### Build
```bash
dotnet build
```
