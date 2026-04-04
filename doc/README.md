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
├── unit_test_plan.md             ← unit test plan with test cases per layer
├── ROADMAP.md                    ← feature roadmap and priorities
├── WALKTHROUGH.md                ← API walkthrough and usage examples
├── DDD_REFACTOR_PLAN.md          ← DDD refactoring plan and status
├── DDD_STRUCTURE.md              ← DDD layer structure reference
├── ENHANCEMENT_PLAN.md           ← planned enhancements
├── EVENT_DRIVEN_PLAN.md          ← event-driven architecture exploration
├── challenge.md                  ← development challenges log
│
├── commands/
│   ├── SETUP_COMMANDS.md         ← environment setup commands
│   └── dotnet-commands.md        ← common dotnet CLI commands
│
├── docker/
│   ├── DOCKER_PLAN.md            ← Docker Compose setup plan
│   └── DOCKER_MESSAGING_PLAN.md  ← async messaging with Docker
│
└── finnhub/
    ├── FINNHUB_FREE_ENDPOINTS.md       ← Finnhub API endpoints reference
    └── FINNHUB_IMPLEMENTATION_TASKS.md ← Finnhub integration tasks
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
