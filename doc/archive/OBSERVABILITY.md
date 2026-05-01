---
description: Central index for logging/tracing/error-handling docs (Seq, CorrelationId, request/response logging).
type: reference
status: active
version: 1.0
tags: [observability, logging, tracing, seq, inventoryalert]
last_updated: 2026-04-26
---

# Observability (Central)

This page centralizes the “how we observe/debug the system” guidance.

## Core docs

- `doc/LOGGING_REVIEW.md` — logging architecture review + request/response logging recommendations.
- `doc/error_handling_and_response_standard.md` — API error response + status code standardization.
- `doc/worker_jobs_and_event_flow_review.md` — Worker jobs + event flow review and hardening plan.

## Can we log UI to Seq?

Yes, but choose the source carefully:

### 1) Next.js server-side logs (recommended)

For logs produced on the server (API routes, server actions, SSR), you can send logs to Seq directly from Node **without exposing the Seq API key to browsers**.

### 2) Browser/client logs (use a proxy)

Do not ship a Seq ingestion API key in frontend code.

Instead:

- UI sends **sanitized** client telemetry/logs to a backend endpoint you own (API).
- API forwards to Seq (adds server-side auth, rate limiting, redaction, and `CorrelationId`).

### 3) Correlation across UI → API → Worker

To make “search the flow by 1 id” work end-to-end:

- UI should propagate `X-Correlation-Id` on outbound requests (or accept it from API responses and reuse it for subsequent calls).
- API stamps `X-Correlation-Id` for every request (already present).
- API must stamp `EventEnvelope.CorrelationId` when publishing async events (required for Worker tracing).

