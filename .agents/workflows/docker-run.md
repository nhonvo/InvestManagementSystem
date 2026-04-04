---
description: Run the application locally with Docker Compose (PostgreSQL + API)
type: workflow
status: active
version: 2.0
tags: [workflow, docker, postgresql, compose, local, inventoryalert]
---

// turbo-all

# /docker-run — Local Stack via Docker Compose

**Objective**: Build and run the full `InventoryAlert.Api` stack (API + PostgreSQL) locally using Docker Compose.

---

## Phase 0: Retrieve Config Context (BM25)

// turbo

1. **Find Docker config files**:
   ```bash
   python .agents/scripts/core/bm25_search.py "docker-compose postgresql api compose" -n 3
   ```

2. **Find appsettings docker config**:
   ```bash
   python .agents/scripts/core/bm25_search.py "appsettings Docker ConnectionStrings Finnhub" -n 3
   ```

---

## Prerequisites

- Docker Desktop running: `docker info`
- `appsettings.Docker.json` configured with Finnhub API key
- Working directory: `InventoryManagementSystem/`

---

## Steps

### 1. Verify Docker is running

```bash
docker info
```

### 2. Build & start all services

```bash
docker-compose up --build -d
```

### 3. Check containers are healthy

```bash
docker-compose ps
```

Expected: `inventoryalert-api` and `postgres` both show `Up`.

### 4. Tail API logs

```bash
docker-compose logs -f api
```

Wait for: `Application started. Press Ctrl+C to shut down.`

### 5. Open Swagger UI

Navigate to: http://localhost:8080/swagger

### 6. Stop all containers

```bash
docker-compose down
```

### 7. Full reset (wipe volumes + DB)

```bash
docker-compose down -v
```

---

## Troubleshooting

| Issue | Fix |
|-------|-----|
| Port 8080 already in use | Change `ports` in `docker-compose.yml` |
| DB connection refused | Wait 5s for Postgres health check, or add `depends_on: condition: service_healthy` |
| Migrations not applied | Check startup logs — `db.Database.Migrate()` runs automatically |
| Finnhub API key missing | Set in `appsettings.Docker.json` under `Finnhub.ApiKey` |
| `docker-compose build` fails | Run `/fix-build` to resolve .csproj issues first |

---

**Parent Context**: `GEMINI.md` · `doc/docker/DOCKER_PLAN.md`
**Next Action**: Test via Swagger → run `/run-tests`
**Keywords**: `docker`, `compose`, `postgresql`, `api`, `local`, `inventoryalert`
