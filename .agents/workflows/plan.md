---
description: Design Feature + Freeze Context (FSD Standard with Spec-Driven Clarification)
type: workflow
status: active
version: 3.0
tags: [workflow, planning, spec, clarification, inventoryalert, ddd]
---

// turbo-all

# /plan — Feature Design & Context Freeze

**Objective**: Fully understand a feature requirement, surface blind spots, design the solution across DDD layers, and freeze context in `doc/` before implementation begins.

---

## Phase 0: Retrieve Context (BM25)

// turbo

1. **Search for related existing work**:
   ```bash
   python .agents/scripts/core/bm25_search.py "<feature keyword> domain service repository" -n 5
   ```

2. **Check architecture constraints**:
   ```bash
   python .agents/scripts/core/bm25_search.py "ddd layer rules application infrastructure" -n 3
   ```

3. **Search for related tests**:
   ```bash
   python .agents/scripts/core/bm25_search.py "<feature keyword> test mock xunit" -n 3
   ```

4. **Check known tech debt** (avoid repeating bugs):
   ```bash
   python .agents/scripts/core/bm25_search.py "tech debt blank entity transaction ExecuteTransactionAsync" -n 3
   ```

---

## Phase 1: Spec-Driven Clarification

Before writing any plan, identify blind spots.

Ask the user these questions (or proceed with defaults if they say "go"):

1. **Scope**: Is this a new method on `ProductService`, a new entity, or a new external integration?
2. **Input/Output**: What does the endpoint receive? What shape does the response need?
3. **Persistence**: Does this require a DB column/table/index change → migration needed?
4. **Finnhub**: Does this feature call Finnhub? If so, how should null/error be handled?
5. **Idempotency**: What happens if the operation is called twice?
6. **Tests**: Which layer tests are required (service / controller / repository)?

**Gate**: Do not proceed to Phase 2 until answers are confirmed or user says "proceed with defaults".

---

## Phase 2: Solution Design

1. **Layer mapping** — decide which files change per DDD layer:

   | Layer | Files to create/modify |
   |-------|----------------------|
   | Domain | `Entities/`, `Interfaces/` |
   | Application | `DTOs/`, `Interfaces/`, `Services/` |
   | Infrastructure | `Repositories/`, `Configurations/`, `External/`, `Workers/` |
   | Web | `Controllers/`, `ServiceExtensions/` |
   | Tests | `Application/Services/`, `Web/Controllers/`, `Infrastructure/` |

2. **Write the spec file** → save to `doc/<feature-id>_spec.md`:
   - Summary (1 paragraph)
   - Affected files list
   - API contract (route, verb, request shape, response shape, status codes)
   - Migration needed? (yes/no + migration name)
   - Test cases (at minimum: happy path, not-found, boundary)

3. **Mermaid flow** (include in spec):
   ```
   Request → Controller → Service → Repository → DB
                                 ↘ FinnhubClient (if external)
   ```

---

## Phase 3: Freeze & Index

1. Save spec to `doc/`:
   ```
   doc/<feature-id>_spec.md
   ```

2. Re-index so BM25 finds the new spec immediately:
   ```bash
   python .agents/scripts/core/bm25_indexer.py
   ```

3. Update `doc/ROADMAP.md` with the new feature entry.

---

## Phase 4: Handoff

Suggest next action:
> *"Plan frozen. Run `/feature-flow` to begin implementation, or `/add-feature` if this is a new method on an existing entity."*

If user confirms, transition directly to Phase 2 of `/feature-flow`.

---

**Parent Context**: `GEMINI.md` · `.agents/skills/ddd-architecture/SKILL.md`
**Next Action**: `/feature-flow` or `/add-feature` or `/add-entity`
**Keywords**: `plan`, `spec`, `clarification`, `design`, `ddd`, `inventoryalert`, `feature`
