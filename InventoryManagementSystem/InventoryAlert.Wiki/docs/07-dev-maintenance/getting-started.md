# Getting Started

## Prerequisites

- Docker Desktop installed and running
- `.env` file with credentials (copy from `appsettings.Example.json`)

## Quick Start

```bash
# From the InventoryManagementSystem/ directory:
docker compose up --build
```

| Service | URL |
|---|---|
| API | http://localhost:8080 |
| UI | http://localhost:3000 |
| Hangfire Dashboard | http://localhost:8080/hangfire |
| Seq Logs | http://localhost:5341 |
| PostgreSQL | localhost:5432 |
| Wiki (local) | http://localhost:3001 |

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
docker exec -it <container> sh   # Open a shell inside a container
```

### .NET API

```bash
dotnet build
dotnet run --project InventoryAlert.Api

# EF Core Migrations
dotnet ef migrations add <MigrationName> --project InventoryAlert.Api
dotnet ef database update        --project InventoryAlert.Api
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

### Unit Tests

```bash
dotnet test InventoryAlert.UnitTests/
```

### Force Rebuild Docker from Scratch

```bash
docker compose down
docker compose build --no-cache
docker compose up
```
