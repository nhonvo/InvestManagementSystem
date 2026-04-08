# Caching Strategy

> How InventoryAlert uses DynamoDB/local cache to reduce API calls and database load.

## What is Cached

| Data | Cache Type | TTL |
|---|---|---|
| Stock Price Quotes | Local in-memory / DynamoDB | 60 seconds |
| Market Status (open/closed) | Local in-memory | 5 minutes |
| Processed Alert Event IDs | DynamoDB | 24 hours |

## Why Cache?

Finnhub API enforces rate limits. By caching the last fetched price, the worker avoids duplicate HTTP calls and ensures alert evaluation uses consistent data within a single sync cycle.
