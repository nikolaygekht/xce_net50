# TextBuffer Fix Plan

**Source**: derived from `LATEST_FINDINGS.md` (Phases 1–3) and `AUDIT_REPORT.md` (current Phase 4 / refactor + tests).
**Status at top of plan**: Phases 1–3 landed; current state is 256/256 tests passing on `main`. Remaining work below.

---

## Design decisions (binding)

These are recorded as the canonical source for *why* the code looks the way it does. Tests pin them; code reviews enforce them.

### D1 — Single-owner callback sink (replaces multi-subscriber `Callbacks`)

Each `TextBuffer` has **one** `ITextBufferCallback Owner`. The owner is the higher-level editor object; it is responsible for fan-out to UI repaint, syntax highlighting, observers, and any other derived listeners.

- **Why**: in practice, every callback consumer needs information that lives on the parent editor object anyway (file path, view, theme, syntax mode). A multi-subscriber collection inside `TextBuffer` was solving a problem the owner has to re-solve regardless.
- **Why not multi-subscriber**: removes thread-safety surface (concurrent add/remove during edit), removes ordering ambiguity, removes duplicate-add semantics, simplifies the codepath in every edit method.
- **Public API**: `TextBuffer.Owner { get; set; }` of type `ITextBufferCallback?`. `null` means no fan-out. `TextBufferCallbackCollection` is removed.

### D2 — Uniform edit semantics: every edit pushes an undo entry and fires a callback

**No detection branching for "is this a no-op?"** Every public edit method always pushes an `IUndoAction` (possibly with empty payload) and always invokes the corresponding `Owner` callback (possibly with `length = 0`).

- **Why**: the cost of one stack entry + one callback for the rare empty edit is far lower than the cost of "is this a no-op?" branching scattered through every edit code path. Uniformity > optimization.
- **Concrete consequences**:
  - `InsertSubstring(0, 0, "")` → pushes an entry, fires `OnSubstringInserted(0, 0, 0)`. `Undo()` of it is a harmless no-op.
  - `DeleteSubstring(0, 5, 0)` → same shape.
  - **Empty transactions** push uniformly too (reverses earlier "empty transactions are not pushed" behaviour).

### D3 — The buffer is conceptually infinite past real content; past-end edits are uniform no-ops

The buffer has a real line count and each line has a real character count, but past those, the buffer is implicitly infinite empty (extra lines, extra trailing spaces). Edits referring to that infinite tail follow uniform rules:

| Operation | Inside real content | Past real end |
|---|---|---|
| `InsertLine(N, ...)` | normal | auto-extends with empty lines (tracked, removed on undo) |
| `DeleteLine(N)` | normal | uniform no-op: empty undo entry + length-0 callback |
| `InsertSubstring(L, C, "x")` | normal | auto-extends with spaces and/or empty lines (tracked, removed on undo) |
| `DeleteSubstring(L, C, k)` | normal | uniform no-op: empty undo entry + length-0 callback |

- **Why**: keeps the "is this index valid?" check in *one* place — the buffer itself — instead of in every caller. Editor code can treat positions past content as legal without guarding.

### D4 — Negative indices throw

`ArgumentOutOfRangeException` from any edit method that receives a negative line or column index. Negatives have no natural "endless" interpretation; they're caller bugs and worth flagging.

### D5 — File I/O preserves the final-newline state byte-for-byte

If the file on disk ends with a newline, the buffer round-trips with that newline. If it doesn't, it doesn't. If it has two trailing newlines, it has two. **No implicit normalization on read or write.**

### D6 — Aggregated editor state, no extension hook (carried over from FIX_PLAN v1)

Cursor, primary block, and markers are first-class members of `TextBuffer`. There is no `IUndoState` plug-in surface. (Implemented in Phase 3; documented here for completeness.)

---

## What's already done

- **Phase 1 (transaction lifecycle)**: peek-then-pop commit, undo/redo blocked while open, `UndoCorruptedException` on action failure.
- **Phase 2 (`BufferStateSnapshot`)**: cursor/block/markers captured before edit and restored after `inner.Undo()` / `inner.Redo()`.
- **Phase 3 (aggregated state)**: `TextCursor`, `buffer.Block`, `buffer.Markers` as first-class members; buffer drives their adjustment in-line.
- **PR1 (refactor — D1/D2/D3/D4 landed 2026-05-08)**:
  - `TextBufferCallbackCollection` deleted; `TextBuffer.Owner` is the sole callback sink.
  - Empty inserts / past-end deletes / empty transactions push uniform no-op undo entries and fire length-0 callbacks.
  - `NoOpUndoAction` introduced as the placeholder entry type.
  - Concurrent multi-subscriber tests retired (surface no longer exists).
- **PR2 (high-priority test gaps — landed 2026-05-08)**:
  - `TextBuffer_CursorSelection` (11 tests): reversed selection, anchor on deleted line, undo/redo round-trip, Collapse/HasSelection invariants, span-vs-adjacent insert.
  - `TextBuffer_ReadApi` (13 tests): span-read boundary contract pinned (non-throwing on out-of-range/negatives).
  - `TextBuffer_UniformNoOp` (7 tests): empty/past-end edits push uniform undo + length-0 callback via `CountingSink`; negative throws don't leave partial entries.
- **PR3 (medium-priority test gaps — landed 2026-05-08)**:
  - `TextBuffer_RedoStateRoundTrip` (4 tests): edit→undo→redo state equality for cursor+block+markers, including through transactions and after repeated cycles.
  - `TextBufferIO_Tests` (+14 tests): D5 final-newline byte-for-byte (LF and CRLF, zero/one/two trailing); explicit-encoding override; null-arg validation; unwritable target.
  - `TextBuffer_StressTests` (+3 tests): 10 MB substring insert + undo/redo, deep auto-extend at line 100,000, combined deep line+column auto-extend.
- **PR4 (low-priority test gaps + small source fix — landed 2026-05-08)**:
  - `TextBuffer_TransactionIdempotence` (2 tests): double-Dispose harmless; manual-then-using exit harmless.
  - `TextBuffer_OwnerContract` (6 tests): null Owner safe; reassignment routes correctly; Owner-throw → exception propagates, buffer consistent, undo entry on stack.
  - **Source change**: `InsertLine`/`InsertSubstring` register the undo entry *before* the Owner callback fires (matching the delete methods); all four public edit methods wrap the inner call in `try/finally` so the snapshot wrap runs even when Owner throws.
- **Test count**: 311/311 (24 in `Scintilla.CellBuffer.Test` + 287 in `Gehtsoft.Xce.TextBuffer.Test`). All four planned PRs landed.

---

## PR1 — Refactor (the only piece that changes shipping behaviour)

This is the load-bearing change. Tests in PR2–PR4 are written against the post-refactor shape.

### 1.1 Replace `Callbacks` collection with `Owner` sink

- Delete `TextBufferCallbackCollection`.
- Add `public ITextBufferCallback? Owner { get; set; }` on `TextBuffer` (default `null`).
- Internal dispatch points become null-checked single calls: `Owner?.OnLinesInserted(...)` etc.
- Migrate the handful of tests still using `buffer.Callbacks.Add(...)` to `buffer.Owner = ...`. Tests that probed *concurrent add/remove* during edit (`TextBuffer_ThreadSafety.ConcurrentCallbackAddAndEdit_ShouldNotThrow`) become obsolete and are removed — that surface no longer exists.

### 1.2 Implement uniform no-op for past-end edits

- `DeleteLine(N)` where `N >= LineCount`: push an `IUndoAction` with empty payload, fire `OnLinesDeleted(N, 0)` (or the equivalent — see 1.4 for action-shape decision), do not touch buffer contents.
- `DeleteSubstring(L, C, k)` where the affected range is entirely past real line content: same — empty undo entry, length-0 callback.
- `DeleteSubstring(L, C, k)` where the range *partially* overlaps real content: clamp `k` to the real-content portion; the action stores only what was actually deleted.

### 1.3 Implement negative-index throws

- `InsertLine(N, ...)`, `DeleteLine(N)`, `InsertSubstring(L, C, ...)`, `DeleteSubstring(L, C, k)` all throw `ArgumentOutOfRangeException` on `N < 0`, `L < 0`, `C < 0`, or `k < 0`.

### 1.4 Empty undo actions push uniformly

- `InsertSubstring(L, C, "")`, `DeleteSubstring(L, C, 0)`, and the past-end variants from 1.2 all push their respective `*UndoAction` types with empty content. Their `Undo()` / `Redo()` are harmless no-ops.
- Empty `UndoTransaction` (no actions added before commit) also pushes uniformly. The existing test that asserts "empty transaction not pushed" is **flipped** to assert it *is* pushed and its `Undo()` is a no-op.

### 1.5 Update CURRENT_STATUS.md and CLAUDE.md

- Reflect the single-owner sink in the API examples.
- Reflect uniform-no-op semantics in the "Known Behaviors" section.

### Acceptance criteria for PR1

- All existing 256 tests still pass after migration (some assertions/setups updated mechanically).
- A grep for `Callbacks.Add(` in the test tree returns zero hits.
- A grep for `TextBufferCallbackCollection` in the source tree returns zero hits.

---

## PR2 — High-priority test gaps

Reframed from `AUDIT_REPORT.md` against the post-PR1 shape.

### 2.1 Cursor anchor and selection under edits + undo/redo (~6–10 tests)

Covering:
- Reversed selection (anchor *after* caret) survives line/substring inserts and deletes.
- Anchor on a deleted line: snaps to deletion point with column 0 (mirrors caret rule); restored exactly on undo.
- Non-collapsed selection round-trips through undo *and* redo (both directions exact).
- `Collapse()` correctness; `HasSelection` invariants.
- Selection that *spans* an inserted region grows; selection adjacent to it does not.

### 2.2 Span-based read API boundaries (~6–8 tests)

Covering `GetLine(Span<char>)`, `GetSubstring(Span<char>)`, and `GetSubstring(string)`:
- Target span shorter than line → truncation; documented return-value contract pinned.
- Target span exactly fits → full copy, no overrun.
- Target span longer than line → unused portion left alone (not zeroed).
- Zero-length target → no-op return.
- Out-of-range line index → throw.
- Past-end column with non-zero length → reads implicit spaces? Or returns zero copied? **Pin whichever the implementation does today** and call it out in the test name.

### 2.3 Uniform-no-op pinning (~4–6 tests)

Inverted from `AUDIT_REPORT.md` §3 per D2/D3. Each asserts an empty undo entry exists *and* a length-0 callback fired:
- `InsertSubstring(0, 0, "")` on a real line.
- `DeleteSubstring(0, 0, 0)` on a real line.
- `DeleteSubstring(0, 99, 5)` on a 4-char line.
- `DeleteLine(99)` on a 3-line buffer.
- Empty `UndoTransaction`.
- Negative indices throw and do *not* push a partial undo entry.

Test infrastructure: a small `CountingSink : ITextBufferCallback` that increments per callback type. Used as `buffer.Owner = sink` and asserted at end.

---

## PR3 — Medium-priority test gaps

### 3.1 Redo state round-trip (~3–4 tests)

Existing tests assert undo restores cursor+block+markers. PR3 mirrors this for redo:
- Edit → undo → redo → cursor/block/markers exactly at the post-edit state.
- Same through a transaction (undo the transaction, then redo, state matches commit-time exactly).

### 3.2 I/O policy pinning (~3–5 tests)

- **Final newline byte-for-byte (D5)**: write a buffer loaded from a file with no trailing newline; round-trip asserts no trailing newline. Same with one. Same with two. Same with file ending mid-line + CRLF vs LF.
- Reader explicit-encoding override: when caller passes an encoding, BOM detection still runs but the override wins.
- Reader/writer argument validation: null path, null metadata, unwritable target.

### 3.3 Large insert / deep auto-extend stress

Mirror existing large-delete coverage:
- 10 MB single-substring insert + undo + redo + content equality.
- Insert at line 100,000 from empty buffer (deep auto-extend) + undo restores empty buffer.

---

## PR4 — Low-priority test gaps

### 4.1 Transaction idempotence (~1–2 tests)

- `using` block exits twice (manually call `Dispose()` then let the `using` exit) → second dispose is harmless, no extra commit, no exception.

### 4.2 Owner contract (~3–4 tests, partly evaporated by D1)

- `Owner = null` → edits succeed, no NRE.
- Owner throws during a callback → buffer state is unchanged (the edit had already mutated state before the callback fired, so the throw propagates *after* the buffer is consistent). Pin whichever guarantee we want here — most likely "exception propagates, buffer is consistent, undo entry is on the stack."
- Owner reassignment between edits: new owner receives subsequent callbacks; old owner does not.

(Note: duplicate-add and concurrent-modify-during-iteration tests from the old multi-subscriber model are gone and won't be replaced.)

---

## Sequencing and risk

| PR | Size | Risk | Notes |
|----|------|------|-------|
| PR1 (refactor) | ~1 day | medium | touches public API surface; reshapes a handful of existing tests |
| PR2 (high-priority tests) | ~1 day | low | additive; depends on PR1 shape |
| PR3 (medium-priority tests) | ~half day | low | additive |
| PR4 (low-priority tests) | ~quarter day | very low | additive, narrow |

**Land PR1 alone**, get it green, then stack PR2–PR4 in order. Don't bundle PR1 with new tests — keep the refactor diff readable.

---

## Explicitly out of scope

- Multi-cursor / multi-selection.
- Multi-byte / grapheme handling.
- Async file I/O.
- Search/replace primitives.
- Undo coalescing.
- Any externally-supplied custom undo-tracked state.
- `*NoLock` internal helpers (no measured contention to justify them).
