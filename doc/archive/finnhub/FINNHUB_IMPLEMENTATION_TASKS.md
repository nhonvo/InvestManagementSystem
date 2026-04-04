# 🛠️ Finnhub Integration Implementation Checklist

Follow these steps to complete the automated pricing and alert system.

---

## 🏗️ Phase 1: Domain & Database
- [x] **Modify `Product` Entity:**
    - Add `string TickerSymbol` (e.g., "AAPL").
    - Add `decimal Price`.
- [x] **Entity Configuration:**
    - Update `OnModelCreating` to ensure these fields are mapped correctly.
- [x] **Migration:**
    - Update `AppDbContext` seeding with Ticker symbols.

---

## 🧩 Phase 2: Application Logic
- [x] **Create `IPricingSyncService`:**
    - Define a method like `Task SyncAllProductPricesAsync(CancellationToken ct)`.
- [x] **Implement `PricingSyncService`:**
    - Inject `IUnitOfWork` and `IFinnhubClient`.
    - **Mapping logic (COMPLETED):**
        - `CurrentPrice` (from Finnhub `c`) -> `Product.Price`
    - **Logic:**
        1. Fetch all products where `TickerSymbol` is not null/empty.
        2. Loop through products and call `_finnhubClient.GetQuoteAsync(p.TickerSymbol)`.
        3. Update `p.CurrentMarketPrice`.
        4. **Alert Calculation:** Check if `(CurrentMarketPrice - OriginalCost) / OriginalCost > 0.2` (20% gain).
        5. If "High Value", log the alert or prepare a notification.
        6. Save changes via `UnitOfWork`.

---

## 🕰️ Phase 3: Background Worker
- [ ] **Create `FinnhubSyncWorker`:**
    - Inherit from `BackgroundService`.
    - Use a `PeriodicTimer` or `Task.Delay` to run every X minutes/hours.
    - Resolve `IPricingSyncService` using a **Scope** (since DbContext is Scoped).
- [ ] **Registration:**
    - Add `builder.Services.AddHostedService<FinnhubSyncWorker>();` in `Program.cs`.

---

## 🚨 Phase 4: Notifications (Bonus)
- [ ] **Event Handling:**
    - Instead of just logging, publish a `HighValueAlertEvent`.
    - Create a simple handler that prints to the console or triggers an email (Mock).

---

## 🧪 Testing Tips
1. **Mock Tickers:** Seed a product with `TickerSymbol = "AAPL"` and a low `OriginalCost` to trigger the "High Value" logic.
2. **Rate Limits:** Finnhub free tier is 60 calls/minute. If you have many products, add a small `Task.Delay` between API calls.
