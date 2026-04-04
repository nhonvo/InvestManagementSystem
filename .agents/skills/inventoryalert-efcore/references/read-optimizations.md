# Query Optimizations

### Tracking Contexts
The highest latency trap in our PostgreSQL setup involves large payloads (e.g., retrieving thousands of products for sync) being inadvertently loaded into the EF Change Tracker.

Any query executed without an accompanying update map **must** append `.AsNoTracking()`.

### EF Core Specific LINQ Functions
When writing LINQ queries against PostgreSQL, prioritize `Microsoft.EntityFrameworkCore.EF.Functions.ILike` over `string.Contains` when checking tickers, as `ILike` takes advantage of native PostgreSQL case-insensitive comparisons without loading the column to memory first.
