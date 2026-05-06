# TextBuffer Fix Plan

**Source**: addresses issues identified in `LATEST_FINDINGS.md`.
**Goal**: make undo/redo round-trip *all* observable buffer state — text, markers, primary selection block, cursor — and harden the transaction lifecycle.

## Design choice: aggregated state, no extension hook

Editor state (cursor, selection block, markers) is owned by `TextBuffer` as first-class members rather than registered externally. There is no `IUndoState` plug-in surface. The earlier external-callbacks design left undo lossy without delivering real extensibility; the user's prior editor (10+ years) never needed an extension hook beyond these three. If the need ever surfaces, an extension surface can be added later as a clean addition.

Concretely after this plan:

```csharp
buffer.Cursor.MoveTo(line, col);             // first-class caret + anchor
buffer.Block.SetStream(0, 5, 10, 20);        // single primary block
buffer.Markers.Add(new TextMarker(...));     // owned by the buffer
buffer.Callbacks.Add(uiRedrawListener);      // ITextBufferCallback stays for non-state listeners
```

`ITextBufferCallback` stays — it remains the right mechanism for UI repaint, syntax highlighting, and similar derived listeners. It is **not** the mechanism for undo-tracked state any more.

---

## Phase 1 — Mechanical lifecycle fixes ✅ DONE

Landed at 206/206 tests:
- 1.1 `CommitTransaction` peek-then-pop.
- 1.2 Reject `Undo`/`Redo` while a transaction is open.
- 1.3 `UndoCorruptedException` + clear stacks when an action throws.

---

## Phase 2 — `BufferStateSnapshot` infrastructure

The mechanism that lets undo round-trip cursor/block/markers. Internal-only — no public extension surface.

### 2.1 Define `BufferStateSnapshot`

- **New file**: `Gehtsoft.Xce.TextBuffer/Undo/BufferStateSnapshot.cs`
- Internal struct (or sealed class) holding:
  - cursor caret + anchor (`int line, col, anchorLine, anchorCol`)
  - block coordinates and type (`TextBufferBlockType, FirstLine, LastLine, FirstColumn, LastColumn`)
  - markers as `(string id, int line, int col)[]` — array of immutable triples taken at capture time
- `Capture(TextBuffer)` static factory; `Restore(TextBuffer)` writes back.
- Restoration of markers writes back into existing marker references by `id` where possible (so external references the user holds stay valid); markers that no longer exist in the snapshot are left in place (won't happen in normal flow because markers are buffer-owned).

### 2.2 `StateSnapshotUndoAction`

- **New file**: `Gehtsoft.Xce.TextBuffer/Undo/StateSnapshotUndoAction.cs`
- Internal `IUndoAction` that holds: `inner` action, `before` snapshot, `after` snapshot.
- **Order of operations** (this is the bug we caught when reviewing the original plan — restore must happen *after* the inner action runs, not before):
  - `Undo()`: `inner.Undo()` runs (callbacks fire, internal state self-adjusts noisily); then `before.Restore(buffer)` overwrites with the correct pre-edit state.
  - `Redo()`: `inner.Redo()` runs; then `after.Restore(buffer)` overwrites with the correct post-edit state.

### 2.3 Capture points

`TextBuffer` wraps top-level edits in `StateSnapshotUndoAction`:

- Public `InsertLine` / `InsertSubstring` / `DeleteLine` / `DeleteSubstring`: when **not** inside a transaction, snapshot before, run, snapshot after, wrap, register.
- `BeginUndoTransaction` (outermost only): snapshot before on `Begin`; on outermost `Commit` snapshot after, wrap the `UndoTransaction`, register.
- Nested transactions and recursive `*Internal` calls (`suppressUndo: true`) **never** snapshot.
- "Where the before-snapshot lives during a transaction": store it alongside the transaction in the existing `mTransactionStack` (change the stack to hold `(UndoTransaction, BufferStateSnapshot)` pairs).

### 2.4 Acceptance criteria for Phase 2

- New types compile; existing 206 tests still pass (snapshot is captured but does nothing observable yet — cursor/block/markers haven't moved into the buffer).
- A scenario test using a temporary stub (or deferred until Phase 3 lands the real consumers) confirms before/after snapshots are taken at the right boundaries.

> Note: Phase 2 alone has limited externally-visible effect — its tests are best written *together with* Phase 3, when there's actual state to round-trip. We'll write Phase 3's scenario tests first, watch them fail, then land 2 + 3 together. Phasing exists for review-readability, not necessarily separate commits.

---

## Phase 3 — Aggregate cursor / block / markers into `TextBuffer`

This is where the real bugs from `LATEST_FINDINGS.md` get fixed.

### 3.1 `TextCursor` as a first-class member

- **New file**: `Gehtsoft.Xce.TextBuffer/TextCursor.cs`
- Holds caret `(Line, Column)` and selection anchor `(AnchorLine, AnchorColumn)`.
- Methods: `MoveTo(line, col)`, `SetSelection(line, col, anchorLine, anchorCol)`, `Collapse()`, `HasSelection`.
- **No `ITextBufferCallback`** — the buffer updates the cursor in-line during edits (no callback indirection needed since cursor is owned).
- Update rules on edits (mirror today's marker rules for line ops; mirror stream-block rules for substring ops):
  - `OnLinesInserted/Deleted`: shift caret + anchor like markers do today.
  - `OnSubstringInserted/Deleted`: shift caret + anchor like a stream block's first/last column do today.
  - Caret on a deleted line → snaps to deletion point with column 0; anchor likewise. Lossy in the forward direction; **but Phase 2 snapshot makes undo restore the original**.
- Exposed as `buffer.Cursor` (getter only — the `TextCursor` object is created and owned by the buffer; consumers mutate it via its methods).

### 3.2 Move `TextBufferBlock` to a single buffer-owned primary block

- Existing `TextBufferBlock` class stays (its forward-adjustment logic is correct).
- `TextBuffer.Block` returns the buffer's single primary block (defaulting to `BlockType.None`).
- Buffer drives block adjustment in-line during edits — no longer dependent on block being added to `Callbacks`.
- Block forward-adjustment behaviour is unchanged from today; **the snapshot covers the lossy "block fully encompassed" case for undo round-trip**.
- Multi-block / multi-cursor support is *not* in scope. If needed later, that's a separate feature.

### 3.3 Move `TextMarkerCollection` to a buffer-owned member

- Existing class stays. Exposed as `buffer.Markers`.
- Buffer drives marker adjustment in-line during edits.
- The snapshot fixes the lost-column-on-line-delete case for undo round-trip.

### 3.4 Remove the now-redundant external registration paths

- `TextMarkerCollection` and `TextBufferBlock` no longer need to be registered via `buffer.Callbacks.Add(...)` — the buffer drives them directly.
- They can keep implementing `ITextBufferCallback` (no harm, may simplify the in-buffer dispatch), or have their callback methods made `internal`. Decide during implementation.
- `buffer.Callbacks` collection itself stays — it's still the right place for UI repaint / highlighter / etc.

### 3.5 API migration

For any existing test or consumer code:

| Before | After |
|--------|-------|
| `var markers = new TextMarkerCollection(); buffer.Callbacks.Add(markers); markers.Add(m);` | `buffer.Markers.Add(m);` |
| `var block = new TextBufferBlock(...); buffer.Callbacks.Add(block);` | `buffer.Block.Set(blockType, firstLine, lastLine, firstCol, lastCol);` |
| (no cursor today) | `buffer.Cursor.MoveTo(line, col);` |

Existing tests need to be updated mechanically. The behavioural assertions on adjustment carry over verbatim — only the setup changes.

### 3.6 Scenario tests (the bug-fix evidence)

Each test describes a real editor flow. Authored failing first; implementation lands until they pass.

- **Marker survives line delete + undo** (the headline bug from `LATEST_FINDINGS.md` §1):
  *"User has a bookmark at line 5 column 10. They delete line 5. They press Ctrl+Z. The bookmark is back at line 5 column 10."*
- **Block survives full-encompass delete + undo** (`LATEST_FINDINGS.md` §2):
  *"User has a stream selection across lines 2–4. They run a 'delete lines 1–5' command. They press Ctrl+Z. The selection is exactly lines 2–4 again."*
- **Cursor returns home on undo** (`LATEST_FINDINGS.md` §3):
  *"User's caret is at line 10 col 5. They run a paste that inserts text at line 0. Caret moves to reflect the shifted content. They press Ctrl+Z. Caret is back at line 10 col 5."*
- **Cursor moves to edit on redo**:
  *"After the undo above, they press Ctrl+Y. Caret is at the post-paste position again."*
- **Cursor + selection round-trip through a transaction**:
  *"User has a multi-line selection. They run 'comment region' which is a transaction of N substring inserts. They press Ctrl+Z. Selection is exactly what it was before."*
- **Forward-adjustment regressions**: the existing 206 tests for marker/block forward adjustment pass with only setup changes (no assertion changes).

---

## Phase 4 — Edge-semantic decisions, docs, and gap-filling tests

Pinning the design choices that today are implicit.

### 4.1 Document and pin block edge semantics

- `TextBufferBlock.OnSubstringInserted/Deleted` boundary rules (`<=` at FirstColumn, `<` at LastColumn): add XML doc explaining *insertion at FirstColumn lands before the block, insertion at LastColumn lands after*.
- Add tests that pin each boundary case so a "drive-by fix" doesn't silently change behaviour.

### 4.2 Document the dual-event nature of auto-extending inserts

- A single `InsertSubstring` past line end fires multiple `OnSubstringInserted` callbacks (one for the auto-spaces, one for the actual text). Document this on the `InsertSubstring` XML comment so listeners know to expect it.

### 4.3 (Optional) `*NoLock` internal helpers

- Only if profiling shows lock contention in undo-action re-entry. Not in scope unless measured.

---

## Order, sizing, and risk

| Phase | Status | Size | Risk |
|-------|--------|------|------|
| 1 | ✅ done | — | very low |
| 2 | pending | ~half day | low (additive infra, snapshot wired but not yet covering anything Phase 1 didn't) |
| 3 | pending | ~2 days | medium (touches markers, blocks; introduces cursor; updates many existing tests' setup) |
| 4 | pending | ~half day | very low (docs + tests) |

**Recommended landing**: 2 + 3 land together as a single feature ("aggregated state with undo round-trip") since their tests are most meaningful in combination. 4 follows.

---

## What this plan deliberately does not address

- Multi-cursor / multi-selection — out of scope; can be added later as a separate feature.
- Multi-byte character / grapheme handling — out of scope per `CLAUDE.md`.
- Async file I/O.
- Search/replace primitives.
- Undo coalescing (merging consecutive single-char inserts). Worth doing eventually for editor UX, but not a correctness fix.
- Any user-supplied custom undo-tracked state — explicitly *not* an extension point. If a real need surfaces, that's a separate design conversation.
