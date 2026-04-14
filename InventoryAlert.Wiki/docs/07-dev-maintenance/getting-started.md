# Getting Started

## Prerequisites

- Docker Desktop installed and running
- `.env` file with credentials (copy values from `appsettings.Example.json`)
- .NET 10 SDK (for local development without Docker)

## Quick Start

```bash
# From the InventoryManagementSystem/ directory:
docker compose up --build
```

| Service | URL | Notes |
|---|---|---|
| API | http://localhost:8080 | Swagger at `/swagger` |
| UI | http://localhost:3000 | Next.js frontend |
| Hangfire Dashboard | http://localhost:8080/hangfire | Admin only |
| Seq Logs | http://localhost:5341 | Structured log viewer |
| PostgreSQL | localhost:**5433** | Internal port 5432 |
| DynamoDB/SQS | localhost:5000 | Moto emulator |
| Redis | localhost:6379 | Cache + dedup |
| Wiki (local) | http://localhost:3001 | Docusaurus dev server |

## Seed Credentials

| Username | Password | Role |
|---|---|---|
| `admin` | `password` | Admin |
| `user1` | `password` | User |

---

## Common Commands Cheat Sheet

### Docker

```bash
docker compose up --build        # Full rebuild and start
docker compose up -d             # Start in background (detached)
docker compose down              # Stop all containers
docker compose down -v           # Stop + wipe all volumes (clears DB!)
docker compose logs -f api       # Tail API logs live
docker compose logs worker       # View worker container logs
docker compose ps                # List running containers and health status
docker compose up -d --build api # Rebuild and restart only the API container
docker exec -it <container> sh   # Open a shell inside a container
```

### .NET API

```bash
dotnet build
dotnet run --project InventoryAlert.Api

# EF Core Migrations (run from InventoryManagementSystem/)
dotnet ef migrations add <MigrationName> \
  --project InventoryAlert.Api \
  --startup-project InventoryAlert.Api

dotnet ef database update \
  --project InventoryAlert.Api \
  --startup-project InventoryAlert.Api
```

### Running Tests

```bash
# All tests
dotnet test

# Unit tests only
dotnet test --filter "FullyQualifiedName~InventoryAlert.UnitTests"

# E2E tests (requires running Docker stack)
dotnet test --filter "FullyQualifiedName~InventoryAlert.E2ETests"

# Integration tests
dotnet test --filter "FullyQualifiedName~InventoryAlert.IntegrationTests"
```

### Next.js UI

```bash
cd InventoryAlert.UI
npm install
npm run dev       # Dev server → http://localhost:3000
npm run build     # Production build
```

### Docusaurus Wiki

```bash
cd InventoryAlert.Wiki
npm run start     # Dev server → http://localhost:3001
npm run build     # Static production build
npm run deploy    # Deploy to GitHub Pages
```

### Force Rebuild Docker from Scratch

```bash
docker compose down -v
docker compose build --no-cache
docker compose up
```
