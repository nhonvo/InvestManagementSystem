---
description: initialize project context and guide on search/docs
type: workflow
status: active
version: 2.0
tags: [workflow, init, bm25, context, onboarding]
---

// turbo-all

# /init — Project Initialization & Context Sync

**Objective**: Rapidly sync with `InventoryAlert.Api` current state, re-index knowledge, and locate critical technical docs before starting any work session.

**Tech Stack**: .NET 10 · EF Core 10 · PostgreSQL · Finnhub API · xUnit + Moq + FluentAssertions

---

## Phase 0: Re-index (BM25)

// turbo

1. **Re-index all project knowledge** (run from project root):

   ```bash
   python .agents/scripts/core/bm25_indexer.py
   ```

2. **Verify core architecture is indexed**:

   ```bash
   python .agents/scripts/core/bm25_search.py "ddd layer architecture domain application infrastructure" -n 3
   ```

3. **Map project tree**:

   ```bash
   tree.com /f | Select-Object -First 60
   ```

---

## Phase 1: Technical Resource Map

Core knowledge locations for this project:

| What | Where |
|------|-------|
| Cold-start briefing | `GEMINI.md` |
| DDD layer rules | `.agents/skills/ddd-architecture/SKILL.md` |
| Testing patterns + Moq recipes | `.agents/skills/testing-patterns/SKILL.md` |
| Finnhub client + worker logic | `.agents/skills/finnhub-integration/SKILL.md` |
| Project coding rules | `.agents/rules/project-rules.md` |
| Unit test plan | `doc/unit_test_plan.md` |
| DDD refactor plan | `doc/DDD_REFACTOR_PLAN.md` |
| Roadmap | `doc/ROADMAP.md` |

---

## Phase 2: BM25 Search Reference

Use BM25 to retrieve focused context without reading full files:

```bash
# Find context about a specific topic
python .agents/scripts/core/bm25_search.py "your query" -n 5

# Search only documentation files
python .agents/scripts/core/bm25_search.py "ProductService transaction" -n 5

# Search with mode filter
python .agents/scripts/core/bm25_search.py "repository pattern EF Core" -n 3 --mode doc

# Limit to a specific folder
python .agents/scripts/core/bm25_search.py "migration" -n 3 -f ".agents/workflows"
```

**Domain-specific queries to know**:
```bash
python .agents/scripts/core/bm25_search.py "ProductService GetPriceLossAlertsAsync"
python .agents/scripts/core/bm25_search.py "ExecuteTransactionAsync unit of work"
python .agents/scripts/core/bm25_search.py "FinnhubClient GetQuoteAsync"
python .agents/scripts/core/bm25_search.py "GenericRepository EF Core CS1998"
```

---

## Phase 3: Verification Gate

```bash
# Build check
cd InventoryManagementSystem
dotnet build

# Test check
dotnet test --no-build --verbosity normal
```

**Success**: Index updated · Architecture located · Build green.

---

**Parent Context**: `GEMINI.md`
**Next Action**: `/feature-flow` or `/plan`
**Keywords**: `init`, `bm25`, `index`, `context`, `onboarding`, `inventoryalert`
