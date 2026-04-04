---
description: use BM25 to retrieve context without reading full files (token saver)
---

// turbo-all

# /search - BM25+ Premium Search (v4.6)

**Objective**: Surgical context retrieval across documentation and code to minimize token waste.

## Phase 0: Intelligent Retrieval (BM25)

**Skills**: `research-expert`, `triage-expert`

// turbo

1. **Smart Search**: `python .agents/scripts/core/bm25_search.py "YOUR_QUERY" -n 5`
2. **Skill Mapping**: Observe the `💡 Recommended Skills` in the output to determine which Expert Persona to adopt.

## Phase 1: Execution Modes

| Mode           | Command                                                       | Objective                               |
| :------------- | :------------------------------------------------------------ | :-------------------------------------- |
| **Default**    | `python .agents/scripts/core/bm25_search.py "query"`          | Search docs (fallback to code).         |
| **Code Only**  | `python .agents/scripts/core/bm25_search.py "query" -m code`  | Search implementation logic.            |
| **Deep Audit** | `python .agents/scripts/core/bm25_search.py "query" --verify` | Show reliability scores.                |
| **Debug**      | `python .agents/scripts/core/bm25_search.py "query" --debug`  | Show hit frequency & technical metrics. |

## Phase 2: Evaluation & Skill Activation

1. **Coordinates**: Identity `file:line` for targeted viewing.
2. **Reliability**: Check match confidence using `--verify`.
3. **Activation**: Use the recommended skills to refine your role for the next task (e.g., Use `nextjs-expert` if the search hit a feature).

## Phase 3: Brain Sync

// turbo

1. **Rebuild**: `python .agents/scripts/core/bm25_indexer.py` after script, doc, or logic updates.

**Next**: `/plan` or `/code`
