# Phase 1 Cleanup — Regex Engine

## Purpose

Phase 1 (`Far.Colorer.RegularExpressions`) shipped a working regex engine with full test coverage (521 passing, 2 known-skipped). However, several issues were left in the implementation that will compound once Phase 2 (TextParser) starts using the engine at scale.

This document is a prerequisite for Phase 2: each item below should be resolved before TextParser implementation begins, because TextParser will multiply the number of regex objects allocated and the frequency of `SetBackReference` calls.

Phase 1 cleanup is small in scope (no new features, no new tests for new behavior — only fixes and refactors with regression coverage).

---

## Items

### 1. Fix the public/internal contradiction in regex documentation

**Problem.** `Docs/Regex/REGEX_OVERVIEW.md` documents `ColorerRegex` as the consumer-facing API (`var regex = new ColorerRegex(...)`, public methods, etc.). However, `ColorerRegex` is declared `internal unsafe class` and is only reachable from `Far.Colorer.Tests` via `InternalsVisibleTo`. The documentation will mislead any future contributor.

**Decision.** `ColorerRegex` stays `internal`. .NET's `System.Text.RegularExpressions` is good enough for editor-side regex search, which was the original reason to expose it. Colorer's regex engine is for HRC/parser internals only.

**Tasks.**
- Move `Docs/Regex/REGEX_OVERVIEW.md` content into a developer-facing internal doc (e.g., a section in `Docs/Regex/REGEX_INTERNAL.md`), or clearly mark the file as "internal API — for engine maintainers only."
- Remove all public-API framing (`public ColorerRegex(...)`) — replace with `internal ColorerRegex(...)`.
- Add a "Why is this internal?" note pointing to `System.Text.RegularExpressions` for general regex use.

**Definition of Done.**
- `REGEX_OVERVIEW.md` either renamed/relocated or rewritten with internal-API framing.
- No public-API examples remain in regex documentation.
- A short rationale paragraph explains the choice.

---

### 2. Fix the `SetBackReference` memory leak

**Problem.** `RegularExpressions/Internal/ColorerRegex.cs:223–250`:

```csharp
SMatches* backTrace = (SMatches*)Marshal.AllocHGlobal(sizeof(SMatches));
// ... populates backTrace ...
matcher.SetBackTrace(backStr, backTrace, namedGroups);
// Note: backTrace is leaked here — in production code we'd need to track and free it
```

Every call leaks a `SMatches` allocation. TextParser's block-end regex is the heaviest consumer of this code path (every block start → end pair invokes `SetBackReference`).

**Tasks.**
- Decide ownership. Two reasonable options:
  - (a) `ColorerRegex` owns the `SMatches*` and frees it in `Dispose` and on the next `SetBackReference` call (replacement semantics).
  - (b) Allocate `SMatches` once per `ColorerRegex` instance, reuse on every call.
- Implement option (b) — it avoids the per-call allocation entirely. Stash `SMatches* backTraceBuffer` as an instance field, allocate in constructor, free in `Dispose`, just rewrite contents in `SetBackReference`.
- Update `CRegExpMatcher.SetBackTrace` if needed so it does not assume ownership of the pointer.

**Definition of Done.**
- No `Marshal.AllocHGlobal` call inside `SetBackReference`.
- `Dispose` frees the single backTrace buffer.
- A regression test creates a regex, calls `SetBackReference` 100k times, asserts steady process memory (e.g., GC.GetTotalMemory delta or process working-set delta within tolerance).
- The "leaked here" comment is removed.

---

### 3. Fix the finalizer/Dispose ordering hazard

**Problem.** `~ColorerRegex` calls `Dispose`, which calls both `compiler.Dispose()` (frees the regex tree on the unmanaged heap) and `matcher.Dispose()` (frees the matches buffer and the backtracking stack). The matcher holds a `SRegInfo* treeRoot` pointer owned by the compiler.

In normal `Dispose` ordering, `compiler.Dispose` is called before `matcher.Dispose`, but the matcher does not access the tree during its `Dispose` — it only frees its own buffers — so this happens to work.

The finalizer path is different: the GC may finalize `compiler` and `matcher` independently and in any order. If `CRegExpCompiler` is finalized while a matcher operation is somehow still in flight (very unlikely, but possible if a caller dropped the `ColorerRegex` reference mid-match on another thread), the tree is dangling.

More importantly, the finalizer walks the same memory the GC may have already touched — finalizers should be defensive.

**Tasks.**
- Remove the finalizer from `ColorerRegex` and only keep it on the leaf classes that *directly* own native memory (`CRegExpCompiler`, `CRegExpMatcher`).
- In each leaf finalizer, guard with a `disposed` flag and use `GC.SuppressFinalize(this)` in `Dispose` (already done in `Dispose` but the flag is missing).
- Document that `ColorerRegex` is *not* safe to drop without `Dispose` if the consumer expects deterministic cleanup — finalizers are best-effort only.

**Definition of Done.**
- `ColorerRegex` no longer has a finalizer (its members handle their own cleanup).
- `CRegExpCompiler` and `CRegExpMatcher` have idempotent `Dispose` (a `disposed` flag prevents double-free).
- A test creates and discards 1000 `ColorerRegex` instances without `Dispose`, forces a GC, and confirms no exceptions/AVs.

---

### 4. Replace per-node `Marshal.AllocHGlobal` with an arena

**Problem.** `CRegExpCompiler.AllocateNode` allocates each `SRegInfo` separately on the unmanaged heap and tracks them in `List<IntPtr>` for cleanup. Real HRC patterns generate dozens of nodes per regex; the parser will compile thousands of regexes across the catalog.

This has several costs:
- Heap fragmentation on the unmanaged heap.
- One free per node on `Dispose`.
- Indirection: each node access is a separate cache line.

**Tasks.**
- Replace per-node allocation with an arena. Two reasonable shapes:
  - (a) `NativeMemory.Alloc` of a single contiguous block sized for the expected node count, bump-pointer allocation within. Grow by doubling if exhausted.
  - (b) A pinned managed `SRegInfo[]` array with index-based references instead of pointers. Loses pointer arithmetic but is GC-friendly and avoids `Marshal.AllocHGlobal` entirely.
- Recommend (a) initially because it requires minimal changes to the matcher (which currently dereferences `SRegInfo*` everywhere). (b) is a larger refactor; defer to a future cleanup if profiling shows arena is fine.
- Update `CharacterClass*` allocation similarly — currently each char class is its own `Marshal.AllocHGlobal`. Either co-locate in the same arena or use a separate arena for character classes.
- Keep `Dispose` semantics: free the entire arena in one call.

**Definition of Done.**
- `CRegExpCompiler` performs at most O(log n) `NativeMemory.Alloc` calls (one per arena growth) instead of O(n).
- All existing regex tests still pass.
- A micro-benchmark (BenchmarkDotNet) compiles a representative HRC regex (~50 nodes) before/after and shows allocation count and time. (Goal: at least 10× fewer alloc calls; speed should be neutral or better.)

---

### 5. Document the per-instance match lock

**Problem.** `ColorerRegex.Match` and `CRegExpMatcher.Parse` both take a per-instance `lock`, serializing all matches against the same compiled regex. This is a *deliberate* choice — the editor parses one file slice at a time — but it is undocumented and surprising.

**Tasks.**
- Add an XML doc comment on `ColorerRegex` (or, if relocated to internal docs, in the dev-facing section) explaining: "A single `ColorerRegex` instance serializes all match operations. Use one instance per parsing thread; do not share across threads to parallelize."
- Remove or reword the `Concurrent...ShouldSucceed` test name in `ConcurrencyStressTest.cs` — the test creates a fresh instance per iteration, so it does not actually test concurrent matches against a shared instance. Rename to clarify ("ConcurrentRegexCompilationAndMatching" or similar).

**Definition of Done.**
- Single-instance threading constraint is documented in code.
- The stress test's name reflects what it actually tests.

---

## Order of Operations

Recommended order (each is independent, but #2 and #3 share files):

1. Item 1 (doc fix) — safe, no code impact.
2. Item 5 (lock documentation) — safe, no behavior change.
3. Item 2 (leak fix) — small, contained.
4. Item 3 (finalizer hardening) — small, contained.
5. Item 4 (arena migration) — largest change; do last so it lands on top of a stable, leak-free engine.

---

## Exit Criteria

Phase 1 cleanup is complete when:
- All 5 items are at "Definition of Done."
- All previously-passing regex tests still pass (521+).
- No new compiler warnings.
- The 2 known-skipped tests remain skipped (PCRE2 empty-branch alternation — out of scope; we target Colorer's actual usage, not full PCRE2 conformance).
