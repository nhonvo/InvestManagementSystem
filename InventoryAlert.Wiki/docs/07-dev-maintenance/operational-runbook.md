# Operational Runbook

> Step-by-step troubleshooting guide for common operational issues.

## Service URLs

| Service | URL | Notes |
|---|---|---|
| API (Swagger) | http://localhost:8080/swagger | REST API + interactive docs |
| UI | http://localhost:3000 | Next.js frontend |
| Hangfire | http://localhost:8080/hangfire | Admin only — job dashboard |
| Seq Logs | http://localhost:5341 | Structured log viewer |
| PostgreSQL | localhost:5433 | `psql -h localhost -p 5433 -U postgres` |
| DynamoDB/SQS | localhost:5000 | Moto (local AWS emulator) |

---

## Check Logs (Seq)

1. Navigate to `http://localhost:5341`
2. Use the search bar: `@Level = 'Error'` to filter errors only
3. Filter by service: `@Properties.SourceContext like '%Worker%'`
4. Filter by correlation: `@Properties.CorrelationId = '<id>'`

---

## Inspect Docker Containers

```bash
docker compose ps                    # List all containers and health status
docker compose logs -f api           # Stream API container logs live
docker compose logs -f worker        # Stream Worker container logs live
docker compose logs seq              # View Seq logs
docker exec -it inventory-api sh     # Enter the API container shell
docker exec -it inventory-worker sh  # Enter the Worker container shell
```

---

## Verify Database Tables

```bash
# Connect to PostgreSQL inside Docker
docker exec -it inventory-db psql -U postgres -d inventoryalert

# Useful queries
SELECT * FROM stock_listings ORDER BY id DESC LIMIT 20;
SELECT * FROM alert_rules WHERE is_active = true;
SELECT * FROM price_history ORDER BY recorded_at DESC LIMIT 50;
SELECT * FROM notifications WHERE is_read = false ORDER BY created_at DESC;
SELECT username, email, role FROM users;
SELECT ticker_symbol, SUM(quantity) FILTER (WHERE type = 0) as bought,
       SUM(quantity) FILTER (WHERE type = 1) as sold
FROM trades GROUP BY ticker_symbol;
```

---

## Common Debug Scenarios

### Login returns 401 "Invalid credentials"
1. Check seed data ran: `SELECT * FROM users;` — should have `admin` and `user1`.
2. If users table is empty: `docker compose down -v && docker compose up --build` to re-seed.

### `GET /api/v1/market/status` returns 404
- Verify the route exists: `GET http://localhost:8080/swagger` → check `MarketController`.
- The `/market/status` route is `[AllowAnonymous]` — no token needed.

### Finnhub returns 401 "Please use an API key"
1. Check `appsettings.json` → `Finnhub:ApiKey` is not empty.
2. In Docker: check the `FINNHUB_API_KEY` env var: `docker exec inventory-api env | grep FINNHUB`.

### Portfolio `POST /positions` returns 400 Bad Request
- The symbol must exist in `StockListing` first. Call `GET /stocks/{symbol}/quote` to trigger discovery, then retry.

---

## Apply EF Core Migrations

```bash
# From InventoryManagementSystem/ directory
dotnet ef migrations add <MigrationName> \
  --project InventoryAlert.Api \
  --startup-project InventoryAlert.Api

dotnet ef database update \
  --project InventoryAlert.Api \
  --startup-project InventoryAlert.Api
```

---

## Force Re-build Docker from Scratch

```bash
docker compose down -v               # Stop + wipe ALL volumes (clears DB!)
docker compose build --no-cache      # Full rebuild of all images
docker compose up                    # Start fresh
```

---

## Trigger a Manual Price Sync (Admin)

```bash
curl -X POST http://localhost:8080/api/v1/stocks/sync \
  -H "Authorization: Bearer <admin-jwt>"
```

Returns `202 Accepted` and enqueues a Hangfire job visible at `/hangfire`.
