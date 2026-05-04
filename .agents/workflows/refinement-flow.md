---
description: Advanced refinement lifecycle for existing features, ensuring end-to-end consistency across Domain, Infrastructure, and UI.
type: workflow
status: active
version: 1.0
tags: [workflow, refinement, stabilization, documentation, gemini]
---

// turbo-all

# /refinement-flow — Unified Refinement & Documentation Lifecycle

**Objective**: Take an existing feature or flow and refine it for better reliability, consistency, and documentation coverage.

---

## Phase 1: Audit & Discovery

1. **Context Mapping**: Use BM25 to find the source code AND the specific review docs/wiki pages.
   ```powershell
   python .agents/scripts/core/bm25_search.py "feature name review spec implementation" -n 8
   ```
2. **Identify Drift**: Compare the current code implementation against the `InventoryAlert.Wiki/docs/` and `doc/` review files.
3. **Analyze Issues**: Look for tech debt, inconsistent naming, or broken delivery paths (e.g., SignalR 404s).

---

## Phase 2: Surgical Implementation

1. **Unify Logic**: Move fragmented logic into shared utilities (e.g., `IAlertRuleEvaluator`) to ensure consistency.
2. **Stabilize Layers**:
   - **Domain**: Update enums and entities for taxonomy changes.
   - **Infrastructure**: Implement deduplication and robust error handling.
   - **UI**: Synchronize global state and add user-facing recovery features (e.g., Refresh buttons).
3. **Clear Errors**: Resolve all compiler warnings and linting issues across .NET and Next.js.

---

## Phase 3: The Multi-Zone Quality Gate

1. **Unit Test Verification**:
   ```powershell
   dotnet test InventoryManagementSystem/InventoryAlert.UnitTests/InventoryAlert.UnitTests.csproj
   ```
2. **Next.js UI Sanity**:
   ```powershell
   cd InventoryAlert.UI
   npm run lint
   # (Optional) npm run build
   ```
3. **Operational Verification**:
   ```powershell
   docker-compose -f InventoryManagementSystem/docker-compose.yml up --build -d
   ```
4. **E2E Validation**:
   ```powershell
   dotnet test InventoryManagementSystem/InventoryAlert.E2ETests/InventoryAlert.E2ETests.csproj
   ```

---

## Phase 4: Documentation & Wiki Sync

1. **Internal Progress**: Update the corresponding `doc/<FEATURE>_REVIEW.md` file with checkmarks.
2. **Wiki Update**: Refine the public-facing docs in `InventoryAlert.Wiki/docs/` to reflect the improved flow.
3. **API Alignment**: Ensure the UI's `api.ts` error handling matches the backend's `ErrorResponse`.

---

## Phase 5: Finalization

1. **Re-index Context**:
   ```powershell
   python .agents/scripts/core/bm25_indexer.py
   ```
2. **Git Commit**: Propose a commit message summarizing the refinement across all layers.

**Keywords**: `refine`, `stabilize`, `sync-docs`, `taxonomy`, `hybrid-evaluation`
