# 🧪 Integration Test Setup Guide (Unified Project Pattern)

## Purpose

This document explains the "Three-Tier" integration testing architecture for the InventoryAlert project, now consolidated into a single **`InventoryAlert.IntegrationTests`** project for better maintainability.

---

## 🏗️ Test Architecture Summary

Instead of separate projects, we use **Internal Directory Tiers** within `InventoryAlert.IntegrationTests`.

### Tier 1: Handler Tests (`/Tests/Handlers` & `/Tests/Services`)
**Target:** Specific service or handler implementations using production DI.
- **Style:** White-box. Resolves handlers directly from the production dependency injection graph.
- **Validation:** **In-memory logs** via `TestLoggerProvider`, WireMock call history, and direct Repository state assertions.
- **Best for:** Complex business logic, edge cases, and verifying interaction with third-party APIs (Finnhub).

### Tier 2: API Tests (`/Tests/Api`)
**Target:** The HTTP surface and middleware pipeline of the API.
- **Style:** Black-box. Sends real HTTP requests to the `inventory-api` container.
- **Validation:** HTTP response codes, JSON payloads, and **Docker container logs** filtered by `X-Correlation-Id`.
- **Best for:** Verifying Auth middleware, routing, status codes, and the full request-to-response lifecycle.

### Tier 3: Whole-Flow Orchestration Tests (`/Tests/Jobs`)
**Target:** The complete system lifecycle and cross-project orchestration.
- **Style:** Black-box / System Integration. Initiates a flow via API and verifies the final result after background processing.
- **Validation:** Monitoring `inventory-worker` logs, message deletion, and **end-to-side-effect** validation (e.g., verifying a notification appears in the API feed).
- **Quality Goal:** Proving that the **API and Jobs projects** work perfectly together to deliver business value. This is the ultimate peak of the quality pyramid.

---

## 🛠️ Infrastructure Services (All Tiers)

| Service | Tool | Purpose |
|---|---|---|
| **AWS Emulator** | Moto | Mock SQS and DynamoDB. Endpoint: `http://localhost:5000` |
| **HTTP Mock** | WireMock | Mock Finnhub API. Endpoint: `http://localhost:9091` |
| **Relational DB** | PostgreSQL | Real database for persistent state. Port: `5433` |

---

## 📈 Assertion Strategy Comparison

| Feature | Tier 1 (Handler) | Tier 2 (Api) | Tier 3 (Jobs) |
|---|---|---|---|
| **Speed** | **Fast** (DI) | Slow (Network) | Medium (Polling) |
| **Determinism** | **High** (In-proc) | Moderate (Timing) | Moderate (Polling) |
| **Log Assertions** | **Direct List** | Docker Scraping | Docker Scraping |

---

## 🚀 Execution Guide

```bash
# Run all tiers
dotnet test

# Run only logic tests (Fast)
dotnet test --filter "Category=Tier1"

# Run only container-based tests (Slower)
dotnet test --filter "Category=Tier2|Category=Tier3"
```
