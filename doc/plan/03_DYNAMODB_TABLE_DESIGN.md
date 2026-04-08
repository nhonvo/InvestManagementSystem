# 03 — DynamoDB Table Design

All DynamoDB tables use **on-demand billing** in development (Moto) and production.
TTL is enabled on every table — the attribute is always `Ttl` (Unix epoch seconds).

---

## Table 2: `inventory-news`

**Purpose:** Company-specific news articles from Finnhub `/company-news`.

| Attribute     | Type   | Key Role    | Notes                           |
|---------------|--------|-------------|---------------------------------|
| `TickerSymbol` | String | HashKey (PK) | e.g. `AAPL`                   |
| `PublishedAt` | String | RangeKey    | ISO 8601, sortable              |
| `Headline`    | String |             |                                 |
| `Summary`     | String |             |                                 |
| `Source`      | String |             | e.g. `Reuters`                  |
| `Url`         | String |             | Full article URL                |
| `ImageUrl`    | String |             | Thumbnail                       |
| `FinnhubId`   | Number |             | Dedup from Finnhub              |
| `Ttl`         | Number | TTL attr    | 30-day expiry                   |

**Access patterns:**
- Query by `TickerSymbol`, filter `PublishedAt` between dates
- Latest N articles: Query + `ScanIndexForward=false` + `Limit=N`

---

## Table 3: `inventory-market-news`

**Purpose:** General market news from Finnhub `/news`.

| Attribute     | Type   | Key Role    | Notes                           |
|---------------|--------|-------------|---------------------------------|
| `Category`    | String | HashKey (PK) | `general`, `forex`, `crypto`, `merger` |
| `PublishedAt` | String | RangeKey    | ISO 8601                        |
| `Headline`    | String |             |                                 |
| `Summary`     | String |             |                                 |
| `Source`      | String |             |                                 |
| `Url`         | String |             |                                 |
| `ImageUrl`    | String |             |                                 |
| `FinnhubId`   | Number |             | Dedup key                       |
| `Ttl`         | Number | TTL attr    | 7-day expiry                    |

---

## Table 4: `inventory-price-history`

**Purpose:** Append-only price snapshots for charting and trend analysis.

| Attribute       | Type   | Key Role    | Notes                           |
|-----------------|--------|-------------|---------------------------------|
| `TickerSymbol`  | String | HashKey (PK) |                                |
| `Timestamp`     | String | RangeKey    | ISO 8601 UTC                    |
| `Price`         | Number |             | Current price                   |
| `Change`        | Number |             | Absolute change                 |
| `ChangePercent` | Number |             |                                 |
| `High`          | Number |             |                                 |
| `Low`           | Number |             |                                 |
| `Open`          | Number |             |                                 |
| `PrevClose`     | Number |             |                                 |
| `Ttl`           | Number | TTL attr    | 90-day expiry                   |

**Access patterns:**
- Query by `TickerSymbol`, get last N snapshots
- Range query for date-range charting

---

## Table 5: `inventory-recommendations`

**Purpose:** Analyst consensus recommendations per symbol per period.

| Attribute    | Type   | Key Role    | Notes                           |
|--------------|--------|-------------|---------------------------------|
| `Symbol`     | String | HashKey (PK) | e.g. `AAPL`                   |
| `Period`     | String | RangeKey    | e.g. `2024-01`                  |
| `StrongBuy`  | Number |             |                                 |
| `Buy`        | Number |             |                                 |
| `Hold`       | Number |             |                                 |
| `Sell`       | Number |             |                                 |
| `StrongSell` | Number |             |                                 |
| `Ttl`        | Number | TTL attr    | 90-day expiry                   |

---

## Table 6: `inventory-earnings`

**Purpose:** Quarterly EPS actuals vs estimates.

| Attribute        | Type   | Key Role    | Notes                           |
|------------------|--------|-------------|---------------------------------|
| `Symbol`         | String | HashKey (PK) |                                |
| `Period`         | String | RangeKey    | e.g. `2024Q1`                   |
| `Actual`         | Number |             | Actual EPS                      |
| `Estimate`       | Number |             | Consensus estimate EPS          |
| `Surprise`       | Number |             | Actual − Estimate               |
| `SurprisePercent`| Number |             |                                 |
| `Ttl`            | Number | TTL attr    | 2-year expiry                   |

---

## Moto (Local Dev) Init Script Additions

Add to `init-all.sh`:

```bash
# inventory-market-news
aws dynamodb create-table \
  --table-name inventory-market-news \
  --attribute-definitions \
    AttributeName=Category,AttributeType=S \
    AttributeName=PublishedAt,AttributeType=S \
  --key-schema \
    AttributeName=Category,KeyType=HASH \
    AttributeName=PublishedAt,KeyType=RANGE \
  --billing-mode PAY_PER_REQUEST \
  --endpoint-url "$ENDPOINT_URL" \
  --region "$AWS_DEFAULT_REGION"

aws dynamodb update-time-to-live \
  --table-name inventory-market-news \
  --time-to-live-specification "Enabled=true,AttributeName=Ttl" \
  --endpoint-url "$ENDPOINT_URL" \
  --region "$AWS_DEFAULT_REGION"

# inventory-price-history
aws dynamodb create-table \
  --table-name inventory-price-history \
  --attribute-definitions \
    AttributeName=TickerSymbol,AttributeType=S \
    AttributeName=Timestamp,AttributeType=S \
  --key-schema \
    AttributeName=TickerSymbol,KeyType=HASH \
    AttributeName=Timestamp,KeyType=RANGE \
  --billing-mode PAY_PER_REQUEST \
  --endpoint-url "$ENDPOINT_URL" \
  --region "$AWS_DEFAULT_REGION"

aws dynamodb update-time-to-live \
  --table-name inventory-price-history \
  --time-to-live-specification "Enabled=true,AttributeName=Ttl" \
  --endpoint-url "$ENDPOINT_URL" \
  --region "$AWS_DEFAULT_REGION"

# inventory-recommendations
aws dynamodb create-table \
  --table-name inventory-recommendations \
  --attribute-definitions \
    AttributeName=Symbol,AttributeType=S \
    AttributeName=Period,AttributeType=S \
  --key-schema \
    AttributeName=Symbol,KeyType=HASH \
    AttributeName=Period,KeyType=RANGE \
  --billing-mode PAY_PER_REQUEST \
  --endpoint-url "$ENDPOINT_URL" \
  --region "$AWS_DEFAULT_REGION"

aws dynamodb update-time-to-live \
  --table-name inventory-recommendations \
  --time-to-live-specification "Enabled=true,AttributeName=Ttl" \
  --endpoint-url "$ENDPOINT_URL" \
  --region "$AWS_DEFAULT_REGION"

# inventory-earnings
aws dynamodb create-table \
  --table-name inventory-earnings \
  --attribute-definitions \
    AttributeName=Symbol,AttributeType=S \
    AttributeName=Period,AttributeType=S \
  --key-schema \
    AttributeName=Symbol,KeyType=HASH \
    AttributeName=Period,KeyType=RANGE \
  --billing-mode PAY_PER_REQUEST \
  --endpoint-url "$ENDPOINT_URL" \
  --region "$AWS_DEFAULT_REGION"

aws dynamodb update-time-to-live \
  --table-name inventory-earnings \
  --time-to-live-specification "Enabled=true,AttributeName=Ttl" \
  --endpoint-url "$ENDPOINT_URL" \
  --region "$AWS_DEFAULT_REGION"
```
