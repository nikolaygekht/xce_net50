# Undo Coalescing — Design

**Status**: design only, not implemented.
**Date**: 2026-07-28
**Scope**: merge consecutive `InsertSubstring` / `DeleteSubstring` edits produced by linear
typing and by Delete/Backspace runs into a single undo entry.

---

## 1. Problem

Typing `hello` calls `InsertSubstring` five times. Per **D2** every public edit pushes an undo
entry, so `RegisterUndoAction` (`TextBuffer.cs`) pushes five, and one Ctrl+Z removes one
character. The old editor grouped linear typing and Delete/Backspace runs into one undo step;
this design restores that.

**In scope**: substring insert runs (typing), substring delete runs (Delete key and Backspace).
**Out of scope**: line-level operations, paste / replace-all / macros (use a transaction —
see §9), merging a Backspace that immediately corrects a typing run (§10.3).

---

## 2. Where it collides with existing decisions

### D2 must be amended

D2 currently reads "every public edit method always pushes an `IUndoAction`". Coalescing means
sometimes the buffer *mutates the top entry* instead of pushing a new one. Proposed replacement
wording:

> **D2** — Every public edit always produces an undo *effect* and always fires its callback,
> even when nothing changed. The effect is normally a new stack entry; when undo coalescing is
> enabled and the edit continues an active run (§4), the effect is merged into the preceding
> entry instead. No edit is ever silently dropped, and no edit path branches on "is this a
> no-op?".

The alternative — keep D2 verbatim and put merging in a layer above `RegisterUndoAction` — was
rejected: it needs a second shadow stack that must stay in sync with the real one.

### D6 makes the cursor available, which helps

Because the buffer owns `Cursor`, it *can* consult the caret when deciding to merge. This design
deliberately **does not**, relying on action geometry instead (§4) — see §5.2 for why, and for
the one case that costs us.

### `StateSnapshotUndoAction` composes cleanly

Every top-level edit is wrapped by `WrapLastUndoActionWithSnapshot` into
`StateSnapshotUndoAction(inner, before, after)`. Merging entries N-1 and N is therefore
well-defined:

```
keep  N-1.before      (cursor/block/markers as they were when the run started)
take  N.after         (…as they are now)
merge N-1.inner with N.inner
```

Undo of a coalesced run lands the caret at the run's start; redo lands it at the run's end.
That is exactly the editor-expected behaviour. Intermediate marker/block states inside the run
are not recoverable — intended, since the run undoes as one unit.

---

## 3. Mergeable action shapes

Only the two substring action types participate. Both currently store what is needed.

| Type | Fields relevant to merging |
|---|---|
| `InsertSubstringUndoAction` | `mLineIndex`, `mColumnIndex`, `mText`, `mAutoAddedLines`, `mAutoAddedSpaces` |
| `DeleteSubstringUndoAction` | `mLineIndex`, `mColumnIndex`, `mDeletedText` |

**Invariant relied on**: a `DeleteSubstringUndoAction` always carries non-empty text — zero-length
and past-end deletes push `NoOpUndoAction` instead (D2/D3). Worth an explicit `Debug.Assert` in
the constructor, because §4.2's two predicates would become ambiguous if empty text were possible.

---

## 4. Merge predicates

Let `prev` be the inner action of the top stack entry and `next` the incoming inner action.
All three rules require `prev.Line == next.Line`.

### 4.1 Typing forward (insert run)

```
prev = Insert(L, Cp, Tp)      next = Insert(L, Cn, Tn)
chain:  Cn == Cp + Tp.Length
merged: Insert(L, Cp, Tp + Tn)
```

Additional requirement: **neither side auto-extended** —
`prev.AutoAddedLines == 0 && prev.AutoAddedSpaces == 0` and likewise for `next`.
`InsertSubstringUndoAction.Undo()` unwinds in three ordered stages (text, then auto-added spaces,
then auto-added lines); a merged text span straddling a padding boundary is not expressible in
the single-action shape, and the merged `Redo()` would re-extend differently. Refusing to merge
auto-extending inserts costs nothing in practice — typing at the caret inside real content never
auto-extends.

### 4.2 Delete runs

Two geometries, because Backspace and Delete differ:

```
Backspace (accumulates leftward)
prev = Delete(L, Cp, Dp)      next = Delete(L, Cn, Dn)
chain:  Cn + Dn.Length == Cp          // next deletion ends where prev began
merged: Delete(L, Cn, Dn + Dp)

Delete key (accumulates at a fixed column)
prev = Delete(L, Cp, Dp)      next = Delete(L, Cn, Dn)
chain:  Cn == Cp                      // deletion repeats at the same spot
merged: Delete(L, Cp, Dp + Dn)
```

Deletes never auto-extend, so no padding guard is needed.

**The two predicates are unambiguous** — both hold only if `Dn.Length == 0`, which §3's invariant
excludes.

**They also compose correctly under interleaving**, which is worth stating because it is not
obvious. Buffer `abcdef`, caret at 3:

```
Backspace → Delete(0,2,"c")                  → "abdef"
Backspace → Delete(0,1,"b")   1+1==2 ✓       → merged Delete(0,1,"bc")   → "adef"
Delete    → Delete(0,1,"d")   1==1   ✓       → merged Delete(0,1,"bcd")  → "aef"
Undo      → insert "bcd" at col 1 into "aef" → "abcdef"  ✓
```

Delete-then-Backspace works the same way. Any interleaving is safe as long as the deleted region
stays contiguous, which is precisely what the two predicates enforce.

### 4.3 Never merged

- Insert against Delete (kind change) — run breaks.
- Different line.
- Either side auto-extended (inserts).
- `NoOpUndoAction` on either side (§5.1 item 4).
- Anything produced by `InsertLine` / `DeleteLine`.
- Anything inside a transaction (transactions already coalesce by construction).

---

## 5. Cut-off conditions

Split by who can actually know the answer. This mirrors **D1**: mechanism in the buffer, policy
in the owner.

### 5.1 Buffer-detected — mandatory, cheap, no configuration

| # | Condition | Why |
|---|---|---|
| 1 | Predicate mismatch (§4) | Not a linear run. Falls out for free: merge fails → new entry → run restarts from it. |
| 2 | `InsertLine` / `DeleteLine` | Structural edit; never part of a typing run. |
| 3 | Auto-extension on either side | §4.1. |
| 4 | `NoOpUndoAction` pushed | e.g. Backspace at column 0. v1: breaks the run. See §10.1 for the "transparent" refinement. |
| 5 | Transaction begin **and** commit | A transaction is its own unit; runs must not straddle either boundary. |
| 6 | `Undo()` / `Redo()` called | The run is over by definition. Also: both already throw while a transaction is open, so no interaction there. |
| 7 | Run length cap reached (§7) | Keeps undo granularity useful and bounds the accumulation buffer. |
| 8 | `UndoCoalescingEnabled` set to `false` | Closes any open run immediately. |

Note that **#1 absorbs most of what "cursor movement" would catch**: if the user arrows away and
types, the column no longer chains, so no merge happens. No cursor inspection required.

### 5.2 Cursor movement — the one gap, and why it is the owner's job

#1 misses exactly one case: *move away and come back*. Type `ab`, press ←←→→ (caret back where it
was), type `c` → the geometry still chains, so it merges, although the user made a deliberate
cursor move and would expect a break.

Three ways to close it:

- **(a) Ignore it.** Rare in practice. Acceptable for v1.
- **(b) Dirty flag on `TextCursor`.** Exact and self-contained, but there is a trap: the buffer
  itself moves the cursor while adjusting it during every edit. Unless buffer-internal adjustment
  is routed separately from external `MoveTo` / `SetSelection`, *every* edit would set the flag and
  no run would ever survive. Doable, fiddly.
- **(c) Owner calls `BreakUndoCoalescing()` from its cursor-move handler.** ← **recommended.**
  Zero buffer coupling, and the owner already distinguishes an arrow key from a character key at
  the point where it handles the keystroke.

Recommendation: ship (a), document (c) as the supported fix, mention (b) with its trap so nobody
implements it without knowing.

### 5.3 Timing — owner-driven

The buffer has **no clock and should not grow one**: a time-dependent buffer is non-deterministic
and awkward to test. Two options:

- **Owner idle timer calling `BreakUndoCoalescing()`** ← recommended. Typical threshold 500 ms–1 s
  (Visual Studio is ~1 s; VS Code leans on cursor-move breaks instead of a timer).
- Inject `Func<long> timeProvider` into `TextBuffer`, `null` (default) meaning no time-based
  breaking. Keeps tests deterministic, but puts policy in the wrong layer. Only if the policy
  must live in the buffer for some reason.

### 5.4 Other owner-driven break points

- **Save point** — break after write, so undo cannot cross a save boundary. Also the natural hook
  for a future "modified since save" flag.
- **Focus loss / window deactivation.**
- **Any command that is not plain typing** — find-replace, reformat, comment-toggle.
- **Paste / replace-all / macro** — do not coalesce these; wrap them in a transaction (§9).

---

## 6. Integration point

One method changes: `WrapLastUndoActionWithSnapshot`. It is already the single place where the
`(before, after)` pair is formed, which makes it the natural merge site.

```
current:  pop inner → capture after → push StateSnapshotUndoAction(inner, before, after)

proposed: pop inner
          capture after
          if (coalescing enabled && run open && top is StateSnapshotUndoAction s
              && TryMerge(s.Inner, inner, out merged))
              pop s; push StateSnapshotUndoAction(merged, s.Before, after)   // keep s.Before
          else
              push StateSnapshotUndoAction(inner, before, after)
              run open = inner is mergeable-capable
```

Requires `StateSnapshotUndoAction` to expose `Inner` and `Before` to the buffer (both `internal`,
so no public surface change).

`RegisterUndoAction` is untouched — in particular `mRedoActions.Clear()` still runs on the first
push of a run, and merging does not re-clear it.

New buffer state:

```csharp
private bool mCoalescingOpen;      // top of mUndoActions may absorb the next edit
private int  mCoalescedRunLength;  // chars accumulated in the current run
```

**Bonus for the Owner**: a merged run replays as one event. Redoing a coalesced `hello` fires a
single `OnSubstringInserted(line, col, 5)` instead of five — strictly better for repaint and
syntax-highlight invalidation.

---

## 7. Accumulation cost and the run cap

Naive `prev.Text + next.Text` on every keystroke is **O(n²)** allocation across a run of n
characters. With a 1024-char cap that is ~500 KB of garbage per run — avoidable and worth
avoiding in an editor's hot path.

**Recommendation**: two dedicated internal types, `CoalescedInsertUndoAction` and
`CoalescedDeleteUndoAction`, each holding a `StringBuilder` plus an anchor column, exposing
`Append` / `Prepend`. The first merge converts the single-shot action into its coalesced form; the
simple types stay simple, and a debugger shows plainly whether an entry is a run.

Backspace runs `Prepend`, which is an O(n) memmove per keystroke — bounded by the cap, so ~500 KB
of *movement* and zero allocation. Fine. If profiling ever disagrees, accumulate reversed and
reverse once at materialization.

`MaxCoalescedRunLength` default **1024** characters.

---

## 8. Public API

```csharp
public bool UndoCoalescingEnabled { get; set; }   // default: false
public int  MaxCoalescedRunLength { get; set; }   // default: 1024
public void BreakUndoCoalescing();                // idempotent; safe when no run is open
```

**Default `false` is deliberate.** Enabling it by default would change the observable undo-entry
count and break existing tests that count entries; opt-in keeps the feature purely additive and
leaves D2's current observable contract intact for callers that do not ask for it. The editor is
also the layer that knows whether it wants run-granularity undo.

`BreakUndoCoalescing()` takes `mLock` and sets `mCoalescingOpen = false`.

---

## 9. Relationship to transactions

Transactions already provide *manual* coalescing, and are the right tool whenever the caller knows
the run's extent up front — paste, replace-all, reformat, macros.

They can also cover interactive typing: open a transaction on the first keystroke, dispose it on a
break condition. **This requires zero library changes and is worth trying before implementing any
of the above.** The one snag: `Undo()` / `Redo()` throw while a transaction is open, so a Ctrl+Z
mid-run means the editor must close the transaction first — a couple of lines in the key handler.

Build the `TryMerge` machinery only if the transaction approach proves awkward in the real editor.
It is strictly more invasive, because it is the only option that requires amending D2.

---

## 10. Deferred refinements

1. **`NoOpUndoAction` transparency** — instead of breaking the run (§5.1 #4), let a no-op be
   absorbed: drop it and keep the run open, so Backspace-at-column-0 in the middle of a delete run
   does not split the entry. Small win, extra special case; not v1.
2. **Cross-line runs** — a typing run that crosses a newline currently breaks (different line).
   Merging across lines would require a compound action; not worth it.
3. **Backspace correcting a typing run** — typing `helo`, Backspace, `lo` is one logical edit to a
   user but breaks on kind change. Some editors shorten the insert run instead of starting a delete
   run. Deferred; needs care so the merged action's `Redo()` stays a single insert.
4. **Coalescing across `Undo`** — never. Undo always ends the run.

---

## 11. Test plan

Mirror the existing per-concern class layout with a `TextBuffer_UndoCoalescing` class.

**Merge geometry**
- Typing run of n chars → one undo entry; undo restores pre-run text; redo reapplies all.
- Backspace run → one entry, correct text and caret both directions.
- Delete-key run → one entry.
- Backspace-then-Delete and Delete-then-Backspace interleavings (§4.2 worked example).
- Non-adjacent insert does **not** merge.
- Insert-then-delete does **not** merge.
- Different-line edits do **not** merge.

**Auto-extension guard**
- Insert past line end (auto-spaces) does not merge with the preceding insert, and neither entry
  is corrupted; undo removes the padding.

**Break conditions**
- `BreakUndoCoalescing()` splits a run; second call is a harmless no-op.
- Transaction begin and commit both break.
- `Undo()` / `Redo()` break.
- Run cap: `MaxCoalescedRunLength + 1` chars → exactly two entries.
- `UndoCoalescingEnabled = false` mid-run closes the run.
- `NoOpUndoAction` (Backspace at column 0) breaks the run — pins the v1 choice so §10.1 is a
  visible, deliberate change if revisited.

**State round-trip**
- Coalesced run: undo restores cursor/block/markers to the run's *start*; redo to its *end*.
- Markers inside the run region survive the round trip.

**Owner contract**
- A coalesced redo fires exactly one `OnSubstringInserted` with the full length (§6).
- Replay-safety bracket still holds: `OnReplayBegin` / `OnReplayEnd` around the flushed events.

**Regression**
- With `UndoCoalescingEnabled = false` (default), all pre-existing entry-count assertions hold —
  i.e. the full existing suite passes unchanged.

---

## 12. Sequencing

| Step | Work | Risk |
|---|---|---|
| 0 | Try the transaction approach in the editor (§9) — may end the project | none |
| 1 | `Debug.Assert` non-empty text in `DeleteSubstringUndoAction` (§3) | none |
| 2 | `IMergeable` + predicates + `Coalesced*UndoAction` types, unit-tested in isolation | low |
| 3 | Wire into `WrapLastUndoActionWithSnapshot`; API from §8, default off | medium — single hot code path |
| 4 | Break conditions §5.1; test class from §11 | low |
| 5 | Amend D2 in `CLAUDE.md`; document owner responsibilities from §5.2–5.4 | none |
