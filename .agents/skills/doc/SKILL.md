---
description: Scaffold and synchronize documentation from templates using BM25 and code audits.
type: workflow
status: active
version: 3.0
tags: [workflow, documentation, bm25, atomic-docs, sync, inventoryalert]
---

// turbo-all

# /doc — Documentation Sync & Brain Freeze

**Objective**: After implementing a feature, synchronize the `doc/` folder with the actual code state using BM25 retrieval and a structured audit. Persist knowledge so future sessions start fast.

---

## Phase 0: Retrieve Existing Docs (BM25)

// turbo

1. **Find existing docs for the topic**:
   ```bash
   python .agents/scripts/core/bm25_search.py "<topic> documentation spec plan" -n 5
   ```

2. **Find related implementation files**:
   ```bash
   python .agents/scripts/core/bm25_search.py "<topic> service repository controller" -n 3
   ```

3. **Check if a spec file already exists**:
   ```bash
   python .agents/scripts/core/bm25_search.py "<feature-id> spec requirements" -n 3 -f "doc"
   ```

---

## Phase 1: Documentation Scaffolding

Create or update the relevant `doc/` file based on what changed:

| Change Type | Doc to update |
|-------------|--------------|
| New entity or feature | `doc/<feature-id>_spec.md` (create) |
| New API endpoint | `doc/WALKTHROUGH.md` (add endpoint) |
| New enhancement | `doc/ENHANCEMENT_PLAN.md` |
| Tech debt tracked | `.agents/GEMINI.md` → Known Tech Debt table |
| Roadmap item | `doc/ROADMAP.md` |

**Doc file format** — every `.md` in `doc/` must have this header:

```yaml
---
description: <one-line summary>
type: spec | plan | reference | walkthrough
status: draft | active | deprecated
version: 1.0
tags: [relevant, keywords, here]
last_updated: YYYY-MM-DD
---
```

---

## Phase 2: Code Audit & Sync

1. **Verify the implementation matches the spec**:
   ```bash
   python .agents/scripts/core/bm25_search.py "<feature> service method" --check-file doc/<feature-id>_spec.md
   ```

2. **Confirm test coverage**:
   ```bash
   dotnet test InventoryManagementSystem --no-build --filter "FullyQualifiedName~<EntityName>"
   ```

3. **Update `GEMINI.md`** if:
   - A new service method was added → update the Key Service table
   - A new tech debt was identified → add to Known Tech Debt table
   - A new slash command was added → update Available Slash Commands

---

## Phase 3: Brain Freeze (Index & Commit)

// turbo

1. **Re-index** so BM25 picks up all new/changed docs:
   ```bash
   python .agents/scripts/core/bm25_indexer.py
   ```

2. **Verify index**:
   ```bash
   python .agents/scripts/core/bm25_search.py "<feature keyword>" -n 3
   ```

3. **Commit** when implementation and docs are stable:
   ```bash
   git add .
   git commit -m "docs(<scope>): sync <feature> documentation"
   ```

---

**Parent Context**: `GEMINI.md`
**Next Action**: `/run-tests` → `/code-review` → push PR
**Keywords**: `doc`, `scaffold`, `sync`, `bm25`, `atomic-docs`, `inventoryalert`, `knowledge`
