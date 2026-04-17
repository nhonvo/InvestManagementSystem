---
description: Combined execution flow from requirement analysis to pushed code
type: workflow
status: active
version: 1.0
tags: [workflow, maintenance, feature, implementation, ddd, context]
---

// turbo-all

# /maintenance-flow — Full Implementation Lifecycle

**Objective**: Execute a task end-to-end, starting from chat requirements, gathering context via BM25, implementing logic, and finishing with stabilization/push.

---

## Phase 0: Requirement & Context Retrieval

1. **Analyze Input**: Capture the core requirement from the USER's recent request.
2. **Retrieve Context (BM25)**:
   ```bash
   # Search for related code and existing wiki documentation
   python .agents/scripts/core/bm25_search.py "<requirement_keywords> wiki controller service" -n 5
   ```
3. **Review Findings**: Parse the retrieved snippets and cross-reference with `InventoryAlert.Wiki/docs/` to ensure architectural alignment.

---

## Phase 1: Implementation

1. **Apply Logic**: Write the code across the necessary DDD layers.
2. **Self-Review**: Ensure no generic placeholders or `Console.WriteLine` are left behind.

---

## Phase 2: Quality Gate (Build & Test)

1. **Full Solution Build**:
   ```powershell
   cd InventoryManagementSystem
   dotnet build InventoryManagementSystem.sln
   ```

2. **Execute Unit Tests**:
   ```powershell
   dotnet test InventoryAlert.UnitTests/InventoryAlert.UnitTests.csproj
   ```

3. **Verify UI Build**:
   ```powershell
   cd ../InventoryAlert.UI
   npm run lint
   npm run build
   ```
4. **Operational Check (Docker)**:
   ```powershell
   docker compose up --build -d
   ```

5. **Execute E2E Tests**:
   ```powershell
   dotnet test InventoryAlert.E2ETests/InventoryAlert.E2ETests.csproj
   ```

**Gate**: `Failed: 0` in all zones.

---

## Phase 3: Documentation Sync (Wiki)

1. **Update API Reference**: Modify `InventoryAlert.Wiki/docs/05-api-services/internal-api.md`.
2. **Update Worker Registry**: Update `InventoryAlert.Wiki/docs/06-background-jobs/workers.md`.
3. **Sync Documentation Structure**:
   - Update `InventoryAlert.Wiki/docs/` (Public Wiki)
   - Update `doc/<feature>_spec.md` (Internal Specs)
   - Update `ROADMAP.md` (Roadmap status)

---

## Phase 4: Git Lifecycle

1. **Branch Configuration**:
   ```powershell
   git checkout -b gem-feat-<slug>
   ```

2. **Stage & Commit**:
   ```powershell
   git add .
   git commit -m "feat(<scope>): <description>"
   ```

3. **Remote Push**:
   ```powershell
   git push origin gem-feat-<slug>
   ```

---

## Phase 5: Brain Freeze

1. **Re-index Search Context**:
   ```powershell
   python .agents/scripts/core/bm25_indexer.py
   ```

**Keywords**: `maintenance`, `implementation`, `bm25-context`, `wiki-sync`, `git-lifecycle`
