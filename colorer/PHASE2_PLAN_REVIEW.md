# Phase 2 Documentation Review

## CLAUDE.md (Project Overview)

### Weak Areas
- **Audience focus not stated**: The document mixes deep C++ implementation details with high-level porting notes, but never clarifies whether its primary reader is a .NET engineer, a systems architect, or a project stakeholder, which makes it hard to know how much context is assumed.
- **Porting guidance gaps**: While each subsystem is described, there is little concrete mapping from the original C++ classes to their intended .NET counterparts (e.g., what namespaces or patterns we expect to recreate), so the overview does not directly help someone plan or estimate the port.
- **Dependencies vs. .NET equivalents**: The “Dependencies” section only re-states the native stack; it does not discuss how those dependencies translate to NuGet packages, OS prerequisites, or any migration risks (licensing, feature gaps).
- **Testing discussion incomplete**: Listing the native tests is useful, but there is no explanation of which ones must be re-created, which can be reused as golden data, or how parity will be validated between the C++ and C# parsers.
- **Risk visibility**: Apart from a short “Key Technical Challenges” list, there is no explicit risk section or mitigation ideas for other subsystems (XML parser, IO abstraction, incremental caching), so the overview cannot be used as an input for planning or staffing decisions.

### Suggested Improvements
- Open with a short “Who should read this” paragraph and an outline that separates “C++ background” from “.NET port implications” so readers can jump to the portion that matters to them.
- For every core component, add a “.NET Target” sub-section that names the planned namespace/class, expected language features (unsafe spans, pooling, etc.), and any deviations from the C++ model.
- Extend the dependency discussion with concrete NuGet/package mappings, highlighting blockers such as ICU APIs that lack direct equivalents and what shim code will be required.
- Expand the testing section into a migration plan: catalog which native tests become reference outputs, what new managed tests are needed, and how cross-language verification (file diffing, snapshot tests) will be automated.
- Add an explicit “Risks and Mitigations” section that covers more than regex (e.g., XML entity handling, cache memory usage, I/O abstraction) so future planning can link mitigation tasks back to the overview.

## PHASE2_PLAN.md (Phase 2 Execution Plan)

### Weak Areas
- **Scope per milestone unclear**: Each “Group” lists dozens of classes without defining what constitutes completion (e.g., API compatibility, parity tests), so weekly targets cannot be measured objectively.
- **Timeline lacks resource assumptions**: The 11-week schedule does not state the team size, level of concurrency, or buffer for unknowns, making the estimate difficult to trust or adjust.
- **Testing strategy disconnected from tasks**: The dedicated testing section is thorough, but the earlier work items never reference when those tests should be authored, so there is a risk that testing is deferred to the end.
- **Integration sequencing risk**: TextParser work (Group 6) depends on Regions/HrcLibrary/IO being stable, yet there are no interim integration checkpoints (e.g., “parse a trivial scheme by Week 4”) to surface defects earlier.
- **Non-functional requirements dispersed**: Performance, memory, and parity constraints are scattered across sections; there is no centralized checklist that engineers can use during reviews or PRs.

### Suggested Improvements
- For each group, add a “Definition of Done” table that states the deliverables (code, documentation, tests) and the verification steps required before exiting that milestone.
- Document staffing assumptions (e.g., two developers + one tester) and show how work streams overlap; without that, stakeholders cannot judge whether the 11-week duration is feasible.
- Embed testing tasks directly into the per-group plans (for example, “Week 2 – KeywordList implementation + unit tests mirroring native KeywordListTest”) so quality work is scheduled, not aspirational.
- Introduce incremental integration goals, such as “End of Week 5: load catalog.xml and parse keywords for a single scheme,” to validate the vertical slice before the full TextParser lands.
- Create a consolidated “Non-Functional Checklist” appendix that engineers must review prior to merge (latency targets, GC pressure guardrails, regression comparison against native output), ensuring these requirements stay visible throughout implementation.
