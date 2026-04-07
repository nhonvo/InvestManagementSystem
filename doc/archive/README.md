# InventoryAlert.Api — Documentation Index
---
description: Master index of all documentation files for the InventoryAlert.Api project
type: reference
status: active
version: 1.0
tags: [doc, index, readme, inventoryalert]
last_updated: 2026-04-04
---

## Structure

```
doc/
├── README.md                     ← this file
│
├── ROADMAP.md                    ← active feature roadmap and priorities
└── archive/                      ← completed or superseded docs
    ├── MODERNIZATION_PLAN.md     ← UI + logging + middleware (Done ✅ 2026-04-05)
    ├── ENHANCEMENT_PLAN.md       ← DDD + code quality improvements (Done ✅ 2026-04-05)
    ├── EVENT_DRIVEN_PLAN.md      ← event-driven architecture (Done ✅)
    ├── EVENT_SETUP_COMMANDS.md   ← event setup commands (Done ✅)
    ├── SETUP_COMMANDS.md         ← initial project setup (Done ✅)
    ├── unit_test_plan.md         ← unit test coverage plan
    ├── DDD_REFACTOR_PLAN.md      ← DDD refactor history
    └── challenge.md              ← completed challenges
```

---

## Doc Status Legend

| Status | Meaning |
|--------|---------|
| `active` | Current, reflects implemented state |
| `draft` | Work-in-progress, not yet implemented |
| `deprecated` | Superseded by newer doc |

---

## Key Docs by Topic

| Topic | Document |
|-------|---------|
| Testing | `unit_test_plan.md` |
| Architecture | `DDD_STRUCTURE.md`, `DDD_REFACTOR_PLAN.md` |
| Roadmap | `ROADMAP.md` |
| API usage | `WALKTHROUGH.md` |
| External API | `finnhub/FINNHUB_FREE_ENDPOINTS.md` |
| Docker setup | `docker/DOCKER_PLAN.md` |
| Dev commands | `commands/dotnet-commands.md` |

---

## How to Update Docs

Always use `/doc` workflow after implementing any feature.

Every `.md` file in this folder should have this header:

```yaml
---
description: <one-line summary>
type: spec | plan | reference | walkthrough
status: draft | active | deprecated
version: 1.0
tags: [relevant, keywords]
last_updated: YYYY-MM-DD
---
```

After updating any doc, re-index BM25:
```bash
python .agents/scripts/core/bm25_indexer.py
```
