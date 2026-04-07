# Implementation Plan: Migrating Logs & News to DynamoDB

Consolidation of high-volume/time-series data into DynamoDB to reduce PostgreSQL load and optimize resource usage.

## 1. Domain & Contracts Layer
- [ ] **NewsRecord Refactoring**: 
    - Update `InventoryAlert.Contracts/Entities/NewsRecord.cs` to include DynamoDB attributes.
    - Set `TickerSymbol` as HashKey and `PublishedAt` (ISO 8601 string) as RangeKey for optimized retrieval.
    - Add TTL support for automatic data cleanup.
- [ ] **NewsDynamoRepository**:
    - Create `INewsRepository` interface in Contracts.
    - Implement `NewsDynamoRepository.cs` using `IDynamoDBContext`.

## 2. Worker Layer (Ingestion)
- [ ] **NewsHandler Update**:
    - Swap `InventoryDbContext` for `INewsRepository`.
    - Persist news directly to DynamoDB.
- [ ] **PollSqsJob Persistence**:
    - Continue using `EventLogDynamoRepository`.
    - (Verification) Ensure it handles de-duplication correctly before saving to DynamoDB.

## 3. API Layer (Querying)
- [ ] **EventLog Queries**:
    - Ensure `IEventLogQuery` implementation (`DynamoDbEventLogQuery`) is fully functional.
    - Update controller endpoints to source data strictly from DynamoDB.
- [ ] **News Queries**:
    - Create `INewsQuery` interface.
    - Implement `DynamoDbNewsQuery` to fetch news per ticker from DynamoDB.
    - Update `NewsController` (or equivalent) to use the new query service.

## 4. Infrastructure Cleanup (PostgreSQL)
- [ ] **DbContext Removal**:
    - Remove `DbSet<EventLog>` and `DbSet<NewsRecord>` from `InventoryDbContext.cs`.
    - Remove associated `IEntityTypeConfiguration` classes.
- [ ] **Migration**:
    - Generate an EF Core migration: `RemoveLogsAndNewsFromPostgres`.
    - Apply migration to drop tables in the development environment.

## 5. Verification
- [ ] Validate news ingestion via simulator.
- [ ] Verify event log querying via API Swagger.
- [ ] Check CloudWatch/LocalStack logs for DynamoDB write success.
