---
description: Working docs for the InventoryAlert solution (specs, plans, references).
type: reference
status: active
version: 1.0
tags: [documentation, bm25, inventoryalert]
last_updated: 2026-04-26
---

# InventoryAlert Documentation Index

This folder contains working documentation for the `InventoryManagementSystem/` solution and related tooling.

## 🗺️ Roadmap & Strategy
- [ROADMAP.md](ROADMAP.md) — Consolidated task list and project progress.
- [feature_audit.md](feature_audit.md) — Current feature-set audit and consolidation notes.
- [architecture_modernization.md](architecture_modernization.md) — Architecture modernization track and completed refactors.

## 🗄️ Archive
- [archive/](archive/) — Legacy plans, setup commands, and hackathon-specific progress logs.

## 📚 Wiki & CI

- [WIKI_REUSE_AND_CI_SYNC.md](WIKI_REUSE_AND_CI_SYNC.md) — Plan to sync `InventoryAlert.Wiki/docs` into the GitHub Wiki repo + suggested CI workflow.

## 🔭 Observability

- [OBSERVABILITY.md](OBSERVABILITY.md) — Central index (Seq + correlation + error/flow docs).
- [LOGGING_REVIEW.md](LOGGING_REVIEW.md) — Logging review + recommendations.
- [error_handling_and_response_standard.md](error_handling_and_response_standard.md) — Error response/status standardization.
- [worker_jobs_and_event_flow_review.md](worker_jobs_and_event_flow_review.md) — Worker jobs + event-flow review.

## 🖥️ UI

- [UI_TECH_SETUP_AND_FLOW.md](UI_TECH_SETUP_AND_FLOW.md) — UI tech stack, setup, folder structure, rendering flow, and Docker approach (doc-only).
- [UI_COMPONENTS_AND_PAGES_AUDIT.md](UI_COMPONENTS_AND_PAGES_AUDIT.md) — Component/page inventory + enhancement suggestions (paging, UX consistency).

## 🔔 Alerts & Notifications

- [NOTIFICATIONS_AND_ALERT_RULE_FLOW_REVIEW.md](NOTIFICATIONS_AND_ALERT_RULE_FLOW_REVIEW.md) — Business flow review + enhancement recommendations.

## 🧪 Testing

- [JOBS_INTEGRATION_TEST_PLAN.md](JOBS_INTEGRATION_TEST_PLAN.md) — Integration test plan for jobs/worker, SQS retries/DLQ, and Option C DLQ reprocessor.

> Run `/doc` to synchronize this index with new feature implementations.
