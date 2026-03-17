---
name: feature-dev
description: >
  Structured 7-phase feature development workflow with specialized agents
  for codebase exploration, architecture design, and quality review.
  Supports resuming across sessions via context snapshots.
  Performance and allocation analysis is applied on every project.
  Use when building new features, designing architecture, or doing complex
  multi-file changes.
  Invoke with: /feature-dev <description>
  Resume with: /feature-dev resume
argument-hint: "<description> | resume"
---

# Feature Dev Skill

## Instructions

### Entry Point

When invoked, first check the argument:

- **`/feature-dev resume`** -> go to **Resume Flow**
- **`/feature-dev <description>`** -> run **Platform Detection**, then go to Phase 1
- **`/feature-dev`** (no args) -> ask: new feature or resume existing?

---

## Platform Detection

Run once at the start of every new task. Determines which platform-specific
performance rules apply.

Scan the repository root for the following signals:

| Signal | Platform |
|--------|----------|
| `*.csproj`, `Assets/`, `ProjectSettings/` | Unity (C#) |
| `build.gradle`, `AndroidManifest.xml`, `*.kt`, `*.java` | Android |
| `*.xcodeproj`, `*.xcworkspace`, `Info.plist`, `*.swift` | iOS |
| none of the above | Generic |

Set `PLATFORM` to the detected value. If multiple signals found, set
`PLATFORM` to the most specific one and note the others.
If detection is ambiguous -> ask the user. **[WAIT]**

Store detected platform in `context.md` under `## Platform`.

> Performance and allocation rules apply to **all platforms**.
> Platform-specific sections provide additional constraints on top of the
> universal rules.

---

## Resume Flow

Goal: Restore context from a previous session and continue from where work stopped.

1. Scan `tasks/` for all `context.md` files.
2. List unfinished tasks (status != `done`):
   ```
   Unfinished tasks:
   1. add-oauth-login     - Phase 5: Implementation (In progress)  [iOS]
   2. rate-limiting-api   - Phase 3: Clarifying Questions (Blocked) [Generic]
   ```
3. Ask user which task to resume. **[WAIT]**
4. Read in order:
   - `tasks/<slug>/context.md` (restore `PLATFORM` from here)
   - `tasks/<slug>/plan.md` (or phase files)
   - `tasks/<slug>/architecture.md` (if exists)
   - `tasks/<slug>/decisions.md` (if exists)
5. Output resume summary:
   ```
   ## Resuming: <Feature Name> [<PLATFORM>]

   ### Where we stopped
   <Last phase and what was being done>

   ### Decisions made so far
   <Key decisions from decisions.md>

   ### Open blockers
   <Blockers section from plan.md>

   ### Next step
   <Exact action to take>
   ```
6. Confirm with user. **[WAIT]**
7. Continue from the indicated phase.

---

## Main Workflow

---

### Phase 1: Discovery

Goal: Understand what needs to be built.

1. If the feature description is vague, ask 2-3 clarifying questions:
   - What problem does this solve?
   - Are there constraints (performance, compatibility, deadlines)?
   - Any preferred approaches or libraries?
2. Ask performance-related questions:
   - Is this on a hot path (game loop, render thread, per-frame update, request handler)?
   - Are there latency or throughput targets?
   - Is GC / allocation pressure a known concern in this area?
3. Assess task complexity:
   - **Simple** - 1-3 files, clear scope -> single plan file
   - **Complex** - multiple subsystems, many files -> split into phase files
4. Derive `<feature-slug>` in kebab-case.
5. Create `tasks/<feature-slug>/` folder.
6. Create initial `context.md` (see format below).
7. Summarize understanding and confirm with user. **[WAIT]**

---

### Phase 2: Codebase Exploration

Goal: Understand relevant existing code, patterns, and performance characteristics.

1. Launch 2-3 `code-explorer` agents **in parallel**:
   - Similar existing features and their implementation patterns
   - Architecture layers and abstractions in the relevant area
   - Related utilities, services, or infrastructure
2. Add a dedicated `code-explorer` agent focused on performance:
   - Identify hot paths and allocation sites in relevant existing code
   - Find existing pooling, caching, or allocation-reduction patterns
   - Platform-specific patterns to look for:
     - **Unity**: NativeArray/Burst usage, Job System, MonoBehaviour lifecycle costs
     - **Android**: RecyclerView pools, Bitmap caching, Handler/Looper patterns
     - **iOS**: ARC retain cycles, dispatch queues, UIKit reuse patterns
     - **Generic**: connection pools, buffer reuse, lazy init patterns
3. Read all key files identified by agents.
4. Present summary including allocation hotspots found and existing
   performance patterns to follow.
5. Update `context.md`.

---

### Phase 3: Clarifying Questions

Goal: Resolve all ambiguities before design begins.

1. Based on Phase 2 findings, identify underspecified aspects:
   - Edge cases and error handling
   - Integration points with existing code
   - Backward compatibility concerns
2. Always include performance clarifications:
   - Maximum acceptable allocations per call on hot path (target: zero)?
   - Can value types / structs be used for key data types?
   - Is object pooling already in use for similar objects?
   - Are there existing benchmarks or profiler baselines to stay within?
3. Present all questions in a numbered list.
4. **[WAIT]**
5. Log answers in `decisions.md` under `## Clarifications`.
6. Update `context.md`.

---

### Phase 4: Architecture Design + Documentation

Goal: Design multiple approaches, let user choose, document the decision.

1. Launch 2-3 `code-architect` agents **in parallel**:
   - **Minimal changes**: maximum reuse, smallest diff
   - **Clean architecture**: best maintainability and separation of concerns
   - **Pragmatic balance**: speed + quality, reasonable trade-offs
2. Every `code-architect` agent must:
   - Mark each component as `hot-path: yes/no`
   - For hot-path components: propose zero-allocation or pool-based design
   - Prefer value types over reference types for short-lived data
   - Avoid allocations, closures capturing heap objects, or boxing on hot paths
   - Platform-specific constraints:
     - **Unity**: avoid `new` in Update/FixedUpdate; prefer `NativeArray`, Burst jobs; use `ScriptableObject` for shared state
     - **Android**: avoid object creation in `onDraw`/`onBindViewHolder`; use `RecyclerView.RecycledViewPool`; prefer `SparseArray` over `HashMap<Integer,*>`
     - **iOS**: avoid retain cycles in closures (`[weak self]`); prefer value types in Swift; avoid dynamic dispatch on hot paths
     - **Generic**: use object pools for frequently created/destroyed objects; prefer stack allocation; cache expensive lookups
3. Present comparison table with pros/cons including allocation profile per approach
   (allocations per call on hot path).
4. **[WAIT]** - ask which approach the user prefers.
5. Log decision in `decisions.md`.
6. Create `tasks/<feature-slug>/architecture.md`:

```markdown
# Architecture: <Feature Name>

> Last updated: <date> | Status: draft | Platform: <PLATFORM>

## Chosen Approach
<Name and rationale>

## Performance Profile
| Component | Hot Path | Allocs/call | Strategy |
|-----------|----------|-------------|----------|
| <name>    | yes/no   | 0 / pooled  | <pool/struct/cached> |

## Components
### <ComponentName>
- **File**: `src/path/to/file`
- **Responsibility**: <what it does>
- **Hot path**: yes / no
- **Interfaces**: <key methods/types>

## Data Flow
<Description or ASCII diagram>

## Key Decisions
| Decision | Choice | Rationale |
|----------|--------|-----------|

## Constraints & Conventions
<Patterns from codebase that must be followed>

## Performance Rules
- No allocations in: <list of methods>
- Use pool for: <list of types>
- Value types preferred for: <list of types>
- <Platform-specific rules>

## Out of Scope
<Explicitly what is NOT included>
```

7. Create plan file(s).
8. Update `context.md`.

---

### Phase 5: Implementation

Goal: Build the feature following the chosen approach.

1. **[WAIT]** - ask for explicit approval to start coding.
2. Read all relevant files from Phases 2-4.
3. Read `tasks/<feature-slug>/architecture.md` before writing any code.
4. Update `context.md`: set phase status to `In progress`.
5. Implement following the chosen architecture, codebase conventions, and
   clarification answers.
6. On every hot-path method enforce:

   **Universal rules (all platforms):**
   - No unnecessary heap allocations on hot paths
   - No closures capturing heap objects on hot paths
   - Prefer value types / structs for short-lived data containers
   - Cache expensive lookups and component references - never repeat them per call
   - Use object pools for frequently created/destroyed objects
   - Avoid boxing (passing value types as reference/interface/object)

   **Unity (C#) additional:**
   - No `new` for reference types in Update/FixedUpdate/LateUpdate
   - No LINQ (`Select`, `Where`, `ToList`, etc.) on hot paths
   - No string concatenation with `+`; use `StringBuilder` or avoid entirely
   - `GetComponent` only in `Awake`/`Start`, never in Update
   - Prefer `NativeArray` + Burst-compiled jobs for CPU-heavy work

   **Android (Kotlin/Java) additional:**
   - No object instantiation in `onDraw`, `onBindViewHolder`, `onMeasure`
   - No autoboxing in tight loops - use primitive arrays
   - Reuse `Paint`, `Rect`, `Path` - allocate once, mutate
   - Use `RecyclerView.RecycledViewPool` for list items

   **iOS (Swift) additional:**
   - Use `struct` over `class` for data types where possible
   - Mark closures `[weak self]` / `[unowned self]` to avoid retain cycles
   - Avoid `Array.map`/`filter`/`flatMap` on hot paths - use manual loops
   - Prefer `ContiguousArray` over `Array` for value types
   - No dynamic dispatch (`@objc`, open protocol) on hot paths

7. After each completed task: update plan checkboxes and Files table.
8. If session ends mid-implementation: fill Blockers, update `context.md`.

---

### Phase 6: Quality Review

Goal: Catch bugs, quality issues, convention violations, architecture drift,
and performance regressions.

1. Launch 5 agents **in parallel**:
   - **Simplicity/DRY/Elegance**: code quality and maintainability
   - **Bugs/Correctness**: logic errors, missing error handling, edge cases
   - **Conventions/Abstractions**: project standards, naming, patterns
   - **Architecture Compliance**: see agent definition below
   - **Performance**: see `performance-reviewer` agent below
2. Consolidate findings. Only report issues with confidence >= 80%.
3. Present findings grouped by reviewer and severity.
4. **[WAIT]** - for each High issue: fix now / fix later / proceed as-is.
5. Log deferred issues in `decisions.md` under `## Deferred`.
6. Apply fixes. Update plan and `context.md`.

---

### Phase 7: Summary

Goal: Document what was accomplished and close out the task.

1. Mark all todos complete in plan files.
2. Update Files table with final state.
3. Update `architecture.md`: set `Status: final`. Log any changed decisions.
4. Update `context.md`: set `status: done`.
5. Output structured summary:
   - **What was built**
   - **Key decisions** (link to `decisions.md`)
   - **Files modified**
   - **Performance notes** - hot-path components and allocation strategies used
   - **Deferred issues**
   - **Suggested next steps**

---

## File Formats

### `tasks/<feature-slug>/context.md`

```markdown
# Context: <Feature Name>

> status: in-progress | done
> current_phase: <1-7>
> last_updated: <timestamp>
> platform: <Unity|Android|iOS|Generic>

## Current Phase
<Phase name> - <Not started | In progress | Blocked | Done>

## Last Action
<One sentence>

## Next Action
<One sentence>

## Blockers
<What is blocking progress. Empty if none.>

## Files Read This Session
- src/path/to/file

## Notes
<Any context that would be lost between sessions>
```

---

### `tasks/<feature-slug>/decisions.md`

Append-only. Never delete entries.

```markdown
# Decisions: <Feature Name>

## Clarifications (Phase 3)
### <topic>
- **Question**: <asked>
- **Answer**: <user said>
- **Impact**: <how this affects implementation>

## Architecture (Phase 4)
### Approach Selection
- **Options considered**: Minimal / Clean / Pragmatic
- **Chosen**: <n>
- **Rationale**: <why>
- **Decided by**: user | agent recommendation accepted

## Implementation (Phase 5)
### <topic>
- **Context**: <situation>
- **Decision**: <what was chosen>
- **Rationale**: <why>
- **Date**: <timestamp>

## Deferred (Phase 6)
### <issue title>
- **Issue**: <description>
- **Location**: file:line
- **Decision**: fix later | proceed as-is
- **Reason**: <why deferred>
```

---

## Agent Definitions

### `code-explorer`
```
You are code-explorer. Your task: [TASK]

1. Find entry points and call chains (file:line references)
2. Map data flow and transformations
3. Identify architecture layers and patterns used
4. List dependencies and integrations
5. Identify allocation sites, hot paths, and existing pool/cache/struct patterns
Return: key files to read, execution flow summary, architecture insights,
performance observations
```

### `code-architect`
```
You are code-architect. Design approach: [APPROACH_TYPE] for [FEATURE].
Platform: [PLATFORM]

Context: [SUMMARY]
Requirements: [REQUIREMENTS]

1. Patterns and conventions to follow
2. Component design with responsibilities
3. Mark each component: hot-path yes/no
4. For hot-path components: specify allocation strategy (pool/struct/cached/zero)
5. Implementation map (specific files to create/modify)
6. Build sequence
7. Pros and cons including allocation profile (allocs/call on hot path)
```

### `code-reviewer`
```
You are code-reviewer. Focus: [FOCUS_AREA]
Review files: [FILES]

- Only report issues confidence >= 80%
- file:line reference for every issue
- Group: High (90-100), Medium (80-89)
- For each issue: problem + suggested fix
```

### `architecture-compliance-reviewer`
```
You are architecture-compliance-reviewer.
Read: tasks/[FEATURE_SLUG]/architecture.md
Review: [FILES]

1. Verify every component exists and is implemented as specified
2. Check data flow matches architecture.md
3. Check "Out of Scope" items were not implemented
4. Verify key decisions were followed
5. Verify "Performance Rules" section was followed

Report:
- COMPLIANT: <component>
- DRIFT: <component> - expected [X], found [Y] (file:line)
- MISSING: <component>
- EXTRA: <component>

Confidence threshold >= 80%.
```

### `performance-reviewer`
```
You are performance-reviewer. Platform: [PLATFORM]
Review hot-path files: [FILES]
Reference: tasks/[FEATURE_SLUG]/architecture.md (Performance Profile section)

Universal checks (all platforms):
- Heap allocations inside methods marked hot-path in architecture.md
- Closures capturing heap objects on hot paths
- Boxing: value types passed as object/interface/reference
- Missing object pools for types allocated frequently
- Expensive lookups (reflection, dictionary miss, DB call) repeated per call
- Uncached results that could be computed once

Unity (C#) additional:
- `new` for reference types in Update/FixedUpdate/LateUpdate
- LINQ on hot paths (Select, Where, ToList, Any, etc.)
- String concatenation with + on hot paths
- GetComponent calls outside Awake/Start
- Missing [BurstCompile] on Job structs where applicable

Android (Kotlin/Java) additional:
- Object instantiation in onDraw, onBindViewHolder, onMeasure, onLayout
- Autoboxing in tight loops (Integer, Long, Double wrappers)
- Paint/Rect/Path allocated per-call instead of reused
- Missing RecycledViewPool for repeated view types

iOS (Swift) additional:
- class where struct is sufficient for hot-path data types
- Missing [weak self]/[unowned self] in stored closures
- Array.map/filter/flatMap on hot paths
- Dynamic dispatch (@objc, open protocol) on hot paths
- Missing ContiguousArray for value type collections

Report format:
- ALLOC: <description> (file:line) - allocation on hot path
- PERF: <description> (file:line) - performance anti-pattern
- SUGGEST: <description> - optimization opportunity (not a blocker)

Confidence threshold >= 80%.
```

---

## tasks/ Folder Convention

```
tasks/
└── <feature-slug>/
    ├── context.md         # Session state + platform
    ├── decisions.md       # Append-only decision log
    ├── architecture.md    # Source of truth (includes Performance Profile)
    ├── plan.md            # Simple tasks
    ├── phase-1-<n>.md     # Complex tasks
    └── phase-N-<n>.md
```

**Status legend:**
- ⬜ Not started
- 🔄 In progress
- 🚫 Blocked
- ✅ Done

---

## When to Use

**Use for:**
- New features touching multiple files
- Features requiring architectural decisions
- Complex integrations with existing code
- Any feature on a hot path regardless of platform

**Skip for:**
- Single-line bug fixes
- Trivial or well-defined simple changes
- Urgent hotfixes
