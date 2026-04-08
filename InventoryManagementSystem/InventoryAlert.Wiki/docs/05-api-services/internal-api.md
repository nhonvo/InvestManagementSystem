# Internal API Reference

> Core REST endpoints exposed by `InventoryAlert.Api`.

## Auth

| Method | Endpoint | Description |
|---|---|---|
| POST | `/auth/register` | Register a new user |
| POST | `/auth/login` | Authenticate and receive JWT tokens |

## Products (Stocks)

| Method | Endpoint | Description |
|---|---|---|
| GET | `/products` | List all tracked stocks |
| POST | `/products` | Add a new stock symbol |
| DELETE | `/products/{id}` | Remove a tracked stock |
| PUT | `/products/{id}/sync-price` | Manually trigger a price sync |

## Alert Rules

| Method | Endpoint | Description |
|---|---|---|
| GET | `/alert-rules` | List all alert rules for current user |
| POST | `/alert-rules` | Create a new alert rule |
| PUT | `/alert-rules/{id}` | Update alert rule |
| DELETE | `/alert-rules/{id}` | Delete an alert rule |

## Watchlists

| Method | Endpoint | Description |
|---|---|---|
| GET | `/watchlists` | Get the user's watchlist items |
| POST | `/watchlists` | Add a stock to the watchlist |
| DELETE | `/watchlists/{id}` | Remove from watchlist |
