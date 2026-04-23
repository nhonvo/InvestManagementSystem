# Alert Rule Creation (Symbol Auto-Resolve)

Alert rules no longer require a “pre-discovered” symbol in the local catalog. When a user creates an alert rule, the API will:

1. Normalize the input symbol (trim + uppercase).
2. Attempt to resolve the symbol via the stock data service (profile lookup).
3. Persist symbol metadata to the local catalog if resolution succeeds.
4. Create the `AlertRule` against the normalized symbol.

This removes the previous coupling where users had to visit a symbol page (or otherwise trigger discovery) before alert creation would succeed.

## Related Endpoint

- `POST /api/v1/alertrules`

## Notes

- The stored `TickerSymbol` is normalized to uppercase.
- If the symbol cannot be resolved, the request fails (see API error response).

