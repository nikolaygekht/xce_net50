# TextBuffer Review — Findings

**Review date**: 2026-05-06
**Scope**: Undo/redo system, with focus on cursor/marker/block correctness during undo and redo.

## Summary

The undo machinery for the **buffer's text content** is sound — line/column auto-extension is tracked correctly, transactions nest properly, the redo stack is cleared on new actions, and stack-allocation of small deleted spans is well thought out. The serious issues are around **state that lives outside the text** — markers, blocks, and (notably absent) cursor position. They rely entirely on the callback stream to "track themselves," and that stream is **lossy** in destructive cases.

---

## Major issues

### 1. Markers lose column information on line delete → undo can't restore it

`TextMarkerCollection.cs:115-117` — when a line containing a marker is deleted, the marker is forced to `(deletedFirstLine, 0)`. The original column is gone.

```
Marker at (5, 10) → DeleteLine(5) → marker becomes (5, 0)
                  → Undo                      → marker becomes (6, 0)   // (5,10) is lost
```

The only existing test (`TextMarker_Tests.cs:454`) only exercises an *insert before the marker* + undo; the lossy case isn't covered, so the bug is silent.

**Fix direction**: marker positions must be snapshotted *before* the operation that may overwrite them, and restored as part of the undo. Either (a) push a dedicated `MarkerStateUndoAction` alongside the data action, or (b) move the marker collection inside the undo system so the callback layer can never lose state.

### 2. Blocks have the same lossy-state problem

`TextBufferBlock.cs:144-150` — when a deletion fully encompasses a block, the code intentionally collapses it (`FirstLine = deletedFirstLine; LastLine = deletedFirstLine - 1`) to mark it invalid but keep its type. There is no path back: the original `FirstLine/LastLine/FirstColumn/LastColumn` are gone, so the inverse `OnLinesInserted` cannot reconstruct them.

Repro:

```
Lines [A,B,C,D,E,F]; Stream block [2,0; 4,5]
Delete lines 1..3 individually (3 separate undo actions)
→ block tracks down through deletes, ends collapsed
Undo×3 → all lines restored, but block ends at the wrong place
```

There are **no** tests covering block restoration through undo (`grep Undo` over `TextBufferBlock_*.cs` returns nothing). This is a coverage gap that hides the bug.

**Fix direction**: same as markers — snapshot block state on operations that may collapse it. A clean approach is a `BlockStateUndoAction` produced when a block is registered as a callback; or, more cleanly, hoist blocks/markers into a "state observer" the undo system explicitly serializes.

### 3. No cursor position tracking in the undo system

This is the biggest functional gap for an editor library. Every undo system that powers a real editor stores **pre-edit cursor** (so cursor returns home on undo) and **post-edit cursor** (so cursor returns to the edit on redo). `IUndoAction` here has no cursor concept — the calling code must manage it externally, and a transaction with multiple operations has no defined "where the cursor was when this group started." Add `before`/`after` cursor (and selection anchor) snapshots either to `IUndoAction` directly, or as a `CursorStateUndoAction` that wraps each user-initiated edit.

### 4. `CommitTransaction` pops before validating

`TextBuffer.cs:607-609`:

```csharp
var currentTransaction = mTransactionStack.Pop();
if (currentTransaction != transaction)
    throw new InvalidOperationException("Transaction mismatch");
```

If the check fails the stack is already mutated — the system is left corrupt and any subsequent transaction logic is wrong. Use `Peek()`, validate, then `Pop()`.

### 5. `Undo()` / `Redo()` are allowed during an open transaction

`TextBuffer.cs:554` and `:570` don't check `mTransactionStack.Count`. Calling `Undo()` mid-transaction pops from `mUndoActions` (which contains pre-transaction history) while the transaction silently continues collecting actions — a "tear" in the history. Should throw `InvalidOperationException` if `mTransactionStack.Count > 0`.

---

## Smaller things worth fixing

- **`UndoTransaction.Undo`/`Redo` is not exception-safe** (`UndoTransaction.cs:31`). If the i-th sub-action throws, the transaction is half-undone and there's no rollback or recovery — the redo stack will redo a partially-consistent snapshot. Either run sub-actions inside try/finally that records progress, or document loudly that sub-actions must not throw.
- **Reentrant locking inside `IUndoAction.Undo()`/`Redo()`**. Actions call `mBuffer.LinesCount` and `*Internal` methods, all of which re-acquire `mLock`. Correct (reentrant `Monitor`), but every call pays the cost. If perf matters, add lock-free `*NoLock` variants for use from within actions.
- **`Box` block + `Stream` block insertion-at-FirstColumn semantics** are baked in (`TextBufferBlock.cs:183`: `<=`). It's a defensible choice (typed text at the cursor positioned at block start lands *outside* the block), but the asymmetry with `<` on `LastColumn` should be documented — otherwise future maintainers will "fix" it.
- **`EnsureColumnExists` fires its own `OnSubstringInserted`** (`TextBuffer.cs:237`), so a single `InsertSubstring` past the line end produces *two* substring events and a single undo action. That's not wrong, but it does mean external listeners see compound events without any "begin/end batch" hint. Worth adding `OnBatchBegin/End` callbacks if listeners need atomicity.
- **Test gaps**: zero block-vs-undo tests, only one marker-vs-undo test (and it tests the easy case). Add tests for: marker on deleted line + undo, block fully-deleted + undo, block partially-deleted + undo, transaction containing line-delete + undo restoring markers/blocks.

---

## Recommendation, prioritized

1. **Add cursor + marker + block snapshotting to the undo pipeline** — this is the architectural change the rest hangs off of. The cleanest version is a small `IUndoState` snapshot interface; each registered "stateful" callback (markers, blocks, cursor) implements `Capture()`/`Restore()`, and `RegisterUndoAction` snapshots them all into a wrapper action.
2. **Fix `CommitTransaction` peek-then-pop, and reject `Undo`/`Redo` inside a transaction.** Five-minute fixes.
3. **Decide and document Stream/Box edge semantics** at boundaries (insert-at-FirstColumn, insert-at-LastColumn). Then add tests pinning them.
4. **Backfill the missing tests** so future regressions in 1–3 are visible.
