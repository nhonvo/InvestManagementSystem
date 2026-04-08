# Operational Runbook

> Step-by-step troubleshooting guide for common operational issues.

## Check Logs (Seq)

1. Navigate to `http://localhost:5341`
2. Use the search bar: `@Level = 'Error'` to filter errors only
3. Use `@Properties.SourceContext like '%Worker%'` to isolate worker logs

## Inspect Docker Containers

```bash
docker compose ps              # List all containers and health status
docker compose logs -f api     # Stream logs from the API container
docker compose logs worker     # View worker container logs
docker exec -it <api_container_name> sh   # Enter the API container shell
```

## Verify Database Tables

```bash
# Connect to PostgreSQL inside Docker
docker exec -it inventoryalert_db psql -U postgres -d inventoryalert

# Useful queries
SELECT * FROM "Products" ORDER BY "Id" DESC LIMIT 20;
SELECT * FROM "AlertRules" WHERE "IsActive" = true;
SELECT * FROM "PriceHistory" ORDER BY "RecordedAt" DESC LIMIT 50;
```

## Apply EF Core Migrations

```bash
dotnet ef database update --project InventoryAlert.Api
```

## Force Re-build Docker

```bash
docker compose down
docker compose build --no-cache
docker compose up
```
