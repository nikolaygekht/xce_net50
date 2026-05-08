# TextBuffer Audit Report

Date: 2026-05-08 (resolution pass: 2026-05-08)

## Scope

Reevaluation of the current `TextBuffer` implementation after the audit-driven fixes landed, with emphasis on realistic programmer-editor scenarios:
- typing and deletion,
- bookmarks / markers,
- undo / redo,
- callback failure behavior during replay.

Current verified status at audit time:
- `dotnet test Gehtsoft.Xce.Text.sln --disable-build-servers -m:1`
- Result: `311/311` tests passing.

Status after resolution pass:
- 329/329 tests passing (24 cell buffer + 305 text buffer).
- Both findings below are resolved; see "Resolution" sections.

## Validated bookmark semantics

Two bookmark scenarios were rechecked directly against the current implementation.

### Scenario 1

Sequence:
- Create bookmark
- Type
- Undo

Observed result:
- bookmark still exists after undo.

### Scenario 2

Sequence:
- Type
- Create bookmark
- Undo

Observed result:
- bookmark disappears after undo.

### Conclusion

This behavior is consistent with the current design: marker membership is restored to the snapshot captured at the edited action boundary. A bookmark that existed before the edit survives undo of that edit; a bookmark added after the edit is removed when that edit is undone.

This is no longer considered an audit finding.

## Findings

1. **Undo/redo is not exception-safe when `Owner` throws during replay (High)** — RESOLVED.
- `StateSnapshotUndoAction` restores buffer-owned state only after `inner.Undo()` / `inner.Redo()` completes.
- The inner undo actions still execute normal buffer edit paths, which still notify `Owner`.
- If `Owner` throws during undo/redo replay, the history is cleared, but text changes already applied are not rolled back.
- This is user-visible corruption risk in a real editor because an observer/UI failure during replay can leave the buffer half-unwound.

Concrete verified case:
- undoing an auto-extended insert after an `Owner` callback throws can leave the auto-added padding spaces behind.

Resolution:
- During `Undo()`/`Redo()` the buffer suppresses `Owner` fan-out and queues the events. The inner action runs end-to-end against the buffer's own internal trackers (cursor, block, markers); the `BufferStateSnapshot` is restored on top; only then are the queued events flushed to `Owner`, bracketed by new `OnReplayBegin` / `OnReplayEnd` signals (default-implemented on `ITextBufferCallback`).
- An `Owner` that throws on `OnReplayBegin` propagates without `OnReplayEnd`. An `Owner` that throws on a flushed event still gets `OnReplayEnd` (in a finally), and the buffer is already in its fully-restored state — no half-unwound corruption.
- Regression test pinning the auto-extended-insert case: `TextBuffer_ReplaySafety.Undo_AutoExtendedInsert_OwnerThrowsOnFirstEvent_BufferFullyRestored`.

2. **Markers do not track substring edits, so column-aware markers drift under normal typing (Medium)** — RESOLVED.
- `TextMarkerCollection` intentionally ignores substring insertions and deletions.
- For line-only bookmarks this may be acceptable.
- For programmer-editor scenarios that use column-bearing markers (diagnostics, search hits, inline anchors, breakpoints with exact columns), this means marker positions become stale as soon as the user types before them on the same line.

Observed behavior:
- inserting text at column 0 does not shift a marker at column 4 on that same line.

Resolution:
- Markers are now column-aware, mirroring `TextCursor` semantics: substring insert at `columnIndex <= marker.Column` on the marker's line shifts the column right by `length`; substring delete entirely before the marker shifts it left; substring delete overlapping the marker clamps the column to the deletion start. Edits on a different line still leave the column untouched.
- Coverage: `TextMarker_Tests` "Substring Operations Tests (column-aware)" region plus three integration tests against the live `TextBuffer` (insert-before-marker, delete-overlapping, undo/redo round-trip).

## Non-findings after reevaluation

The earlier concern that undo incorrectly removes bookmarks added after an edit is withdrawn.

Given the validated semantics above, the current behavior is coherent:
- bookmarks created before an edit survive undo of that edit,
- bookmarks created after an edit disappear when that edit is undone.

## Overall assessment

The audit-driven refactor and test additions substantially improved the project. Core text undo/redo, transaction lifecycle, cursor/block/marker snapshot restoration, and the previously identified structural gaps appear to be in good shape.

After the resolution pass, both remaining findings are addressed:
- replay is now atomic from the buffer's point of view, with `OnReplayBegin` / `OnReplayEnd` bracketing the flushed events;
- markers track substring edits using the same rules as the cursor.
