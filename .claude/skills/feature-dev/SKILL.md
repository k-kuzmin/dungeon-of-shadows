---
name: feature-dev
description: >
  Structured 7-phase feature development workflow with specialized agents
  for codebase exploration, architecture design, and quality review.
  Supports resuming across sessions via context snapshots.
  Use when building new features, designing architecture, or doing complex
  multi-file changes.
  Invoke with: /feature-dev <description>
  Resume with: /feature-dev resume
argument-hint: "<description> | resume"
---

# Feature Dev Skill

## Entry Point

- **`/feature-dev resume`** -> Resume Flow
- **`/feature-dev <description>`** -> Phase 1
- **`/feature-dev`** (no args) -> ask: new feature or resume?

---

## Resume Flow

1. Scan `tasks/*/context.md`, list unfinished (status != done).
2. Ask which to resume. **[WAIT]**
3. Read: `context.md`, `plan.md`, `architecture.md`, `decisions.md` (if exist).
4. Output: where stopped, decisions so far, blockers, next step.
5. Confirm. **[WAIT]** Continue from indicated phase.

---

## Performance Rules (C# / Raylib)

Единый блок — ссылаться из Phase 4, 5, 6.

- No heap allocations on hot paths (Tick, Draw)
- No LINQ (`Select`, `Where`, `ToList`) on hot paths
- No string concatenation with `+` on hot paths; no `Enum.ToString()` per frame
- Prefer struct over class for short-lived data
- Cache component refs and lookups — never repeat per tick
- Reuse `List<int>` buffers via `World.QueryInto(...)`, no per-frame `new List`
- No closures capturing heap objects on hot paths
- No boxing (value type as object/interface)
- Use object pools for frequently created/destroyed objects

---

## Phases

### Phase 1: Discovery

1. If vague — ask: what problem, constraints, preferred approach?
2. Ask: hot path (game loop/render)? GC concern? Allocation targets?
3. Assess complexity: **Simple** (1-3 files) -> single plan. **Complex** -> phase files.
4. Create `tasks/<feature-slug>/context.md`.
5. Summarize, confirm. **[WAIT]**

### Phase 2: Codebase Exploration

1. Launch 2-3 `code-explorer` agents in parallel:
   - Similar features and patterns
   - Architecture layers and abstractions
   - Allocation sites, hot paths, existing buffer/cache/struct patterns
2. Read key files identified by agents.
3. Present summary including perf observations. Update `context.md`.

### Phase 3: Clarifying Questions

1. From Phase 2 findings, identify: edge cases, integration points, backward compat.
2. Include: max allocations on hot path (zero?), struct vs class for key types, existing pools/benchmarks?
3. Present numbered list. **[WAIT]**
4. Log in `decisions.md`. Update `context.md`.

### Phase 4: Architecture Design

1. Launch 2-3 `code-architect` agents in parallel:
   - **Minimal** (smallest diff, max reuse)
   - **Clean** (best maintainability)
   - **Pragmatic** (speed + quality)
2. Each agent: mark components hot-path yes/no, propose allocation strategy per Performance Rules.
3. Present comparison + allocation profile. **[WAIT]**
4. Log in `decisions.md`. Create `architecture.md` (see template). Create plan. Update `context.md`.

### Phase 5: Implementation

1. **[WAIT]** — explicit approval to start.
2. Read architecture.md + relevant files from prior phases.
3. Implement following architecture, conventions, Performance Rules.
4. Update plan checkboxes + Files table after each task.
5. If session ends mid-work: fill Blockers, update `context.md`.

### Phase 6: Quality Review

1. Launch 3-5 `code-reviewer` agents in parallel:
   - Simplicity / DRY / Elegance
   - Bugs / Correctness
   - Conventions / Abstractions
   - Architecture compliance (vs architecture.md)
   - Performance (vs Performance Rules + architecture.md hot-path markers)
2. Only report confidence >= 80%. Group by severity (High 90+, Medium 80+).
3. **[WAIT]** — for High issues: fix now / later / as-is.
4. Log deferred in `decisions.md`. Apply fixes. Update `context.md`.

### Phase 7: Summary

1. Mark todos complete. Update Files table. Set architecture.md `Status: final`.
2. Set `context.md` status: done.
3. Output: what built, key decisions, files modified, perf notes, deferred issues, next steps.

---

## Agent Prompts

**code-explorer:**
> Find entry points, call chains, data flow, architecture layers, allocation sites, hot paths, existing pool/cache/struct patterns for [TASK]. Return: key files, execution flow, architecture insights, perf observations.

**code-architect:**
> Design [APPROACH_TYPE] for [FEATURE]. Context: [SUMMARY]. Requirements: [REQUIREMENTS]. Mark components hot-path yes/no. For hot-path: allocation strategy (pool/struct/cached/zero). Provide: patterns to follow, component design, implementation map, build sequence, pros/cons with allocs/call.

**code-reviewer:**
> Focus: [FOCUS_AREA]. Files: [FILES]. Only confidence >= 80%. file:line refs. Group: High (90+), Medium (80+). Problem + fix.

**architecture-compliance:**
> Read architecture.md. Review [FILES]. Check: components exist as specified, data flow matches, Out of Scope not implemented, Performance Rules followed. Report: COMPLIANT / DRIFT / MISSING / EXTRA. Confidence >= 80%.

**performance:**
> Review hot-path files [FILES] vs architecture.md Performance Profile. Check per Performance Rules section above. Report: ALLOC (hot-path allocation), PERF (anti-pattern), SUGGEST (opportunity). Confidence >= 80%.

---

## File Templates

### context.md
```markdown
# Context: <Feature>
> status: in-progress | done
> current_phase: <1-7>
> last_updated: <date>

## Current Phase — <status>
## Last Action — <one sentence>
## Next Action — <one sentence>
## Blockers
## Files Read
## Notes
```

### decisions.md
Append-only.
```markdown
# Decisions: <Feature>
## Clarifications (Phase 3)
### <topic> — Question / Answer / Impact
## Architecture (Phase 4)
### Approach — Options / Chosen / Rationale
## Implementation (Phase 5)
### <topic> — Context / Decision / Rationale / Date
## Deferred (Phase 6)
### <issue> — Description / Location / Decision / Reason
```

### architecture.md
```markdown
# Architecture: <Feature>
> Last updated: <date> | Status: draft/final

## Chosen Approach — <rationale>
## Performance Profile
| Component | Hot Path | Allocs/call | Strategy |
## Components
### <Name> — File, Responsibility, Hot path, Interfaces
## Data Flow
## Key Decisions
## Constraints & Conventions
## Performance Rules — <methods with no allocs, pooled types, struct types>
## Out of Scope
```
