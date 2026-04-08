# External Service Integrations

> How InventoryAlert communicates with third-party APIs.

## Finnhub API

- **Purpose**: Real-time stock quote data
- **Endpoint**: `GET https://finnhub.io/api/v1/quote?symbol={ticker}&token={API_KEY}`
- **Response fields used**: `c` (current price), `t` (timestamp)
- **Rate Limits**: 60 requests/minute on free tier
- **Error handling**: If `c` is `null` or `0`, the sync is skipped and a warning is logged

## Telegram Bot API

- **Purpose**: Send push notification when an alert fires
- **Endpoint**: `POST https://api.telegram.org/bot{TOKEN}/sendMessage`
- **Body**: `{ chat_id, text }`
- **Failure policy**: Log error and continue — alerts are NOT retried on Telegram failure

## Amazon SQS

- **Purpose**: Internal event bus between Api and Worker
- **Usage**: `PriceSyncedEvent`, `AlertTriggeredEvent` messages
- **Consumer pattern**: Long-polling with visibility timeout
