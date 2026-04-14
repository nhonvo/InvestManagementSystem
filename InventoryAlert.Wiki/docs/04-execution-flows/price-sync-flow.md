# Price Sync Flow

> Orchestration pipeline for global market synchronization, alert evaluation, and in-app notification delivery.

## Sequence (v2 Architecture)

```mermaid
sequenceDiagram
    participant HF as Hangfire Scheduler
    participant Job as SyncPricesJob
    participant Finnhub as Finnhub API
    participant DB as PostgreSQL
    participant Redis as Redis Cache
    participant SQS as Amazon SQS

    HF->>Job: Trigger (every 15 minutes)
    Job->>DB: SELECT DISTINCT TickerSymbols FROM StockListing

    loop For each symbol
        Job->>Redis: GET quote:{symbol}
        alt Cache HIT (< 30s)
            Redis-->>Job: cached quote
        else Cache MISS
            Job->>Finnhub: GET /quote?symbol={symbol}
            Finnhub-->>Job: { currentPrice, change, ... }
            Job->>Redis: SET quote:{symbol} TTL=30s
        end

        Job->>DB: INSERT INTO PriceHistory

        Note over Job,DB: Alert Evaluation
        Job->>DB: SELECT active AlertRules WHERE TickerSymbol = symbol
        loop For each active AlertRule
            alt Condition == PercentDropFromCost
                Job->>DB: SELECT Trades WHERE UserId = rule.UserId AND TickerSymbol = symbol
                Job->>Job: Compute cost basis + unrealized loss %
            end
            alt Rule is breached
                Job->>Redis: GET cooldown:alert:{symbol}
                alt Not in cooldown
                    Job->>DB: INSERT Notification { UserId, Message, TickerSymbol }
                    Job->>Redis: SET cooldown:alert:{symbol} TTL=24h
                    Job->>SQS: Publish inventoryalert.pricing.price-drop.v1
                    alt TriggerOnce = true
                        Job->>DB: UPDATE AlertRule SET IsActive = false
                    end
                end
            end
        end
    end
    Job->>DB: SaveChangesAsync
```

---

## Alert Trigger Conditions

| Condition | Evaluation Logic |
|---|---|
| `PriceAbove` | `quote.CurrentPrice > rule.TargetValue` |
| `PriceBelow` | `quote.CurrentPrice < rule.TargetValue` |
| `PriceTargetReached` | `|quote.CurrentPrice - rule.TargetValue| < tolerance` |
| `PercentDropFromCost` | `(costBasis - currentPrice) / costBasis * 100 >= rule.TargetValue` |
| `LowHoldingsCount` | `SUM(BuyQty) - SUM(SellQty) < rule.TargetValue` |

---

## Logic Highlights

| Feature | Detail |
|---|---|
| **Global Normalization** | `SyncPricesJob` fetches only distinct tickers. 50 users watching AAPL = 1 Finnhub call. |
| **30s Quote Cache** | Prevents duplicate Finnhub calls within a single sync cycle. Key: `quote:{symbol}`. |
| **Trade-Based Cost Basis** | `PercentDropFromCost` evaluates against the user's actual bought positions, not a stale snapshot. |
| **In-App Notification** | Alert breaches write to the `Notification` table — UI badge updates instantly. |
| **24h Cooldown** | `cooldown:alert:{symbol}` Redis key prevents repeated alerts within 24 hours for the same symbol. |
| **TriggerOnce** | If `rule.TriggerOnce = true`, rule is disabled automatically after first breach. |
| **User Isolation** | `LowHoldingsCount` and `PercentDropFromCost` always filter by `(UserId, TickerSymbol)`. Never aggregate across users. |

---

## SQS Event Payload

The `inventoryalert.pricing.price-drop.v1` event published by `SyncPricesJob` follows the `EventEnvelope` pattern:

```json
{
  "eventType": "inventoryalert.pricing.price-drop.v1",
  "correlationId": "...",
  "payload": {
    "symbol": "TSLA"
  }
}
```

`PriceAlertHandler` in the Worker consumes this and performs a secondary evaluation to confirm the breach is still valid before triggering any additional side-effects (e.g., third-party relay).
