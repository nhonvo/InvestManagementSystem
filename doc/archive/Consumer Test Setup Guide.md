# Consumer Test Setup Guide

## Purpose

This document explains how the Consumer repository test setup works so the same pattern can be reused in another project.

The repository contains 3 test styles:

- `src/CAL.Consumer.Api.Tests` - API integration tests
 - `src/CAL.Consumer.Jobs.Tests` - background worker / queue integration tests
 - `src/CAL.Consumer.Handler.Tests` - direct handler integration tests

## Test Architecture Summary

### `CAL.Consumer.Api.Tests`

Use this test project when the target is the HTTP surface of the API.

Main characteristics:

- sends real HTTP requests to the API
 - uses seeded DynamoDB data
 - uses SQS test helpers
 - validates behavior through response payloads and container logs
 - depends on the `consumer_api` container being available

Important setup files:

- `src/CAL.Consumer.Api.Tests/Shared/Config/TestFixture.cs`
 - `src/CAL.Consumer.Api.Tests/Shared/Config/TestBeforeAfterCustom.cs`
 - `src/CAL.Consumer.Api.Tests/Shared/Config/ActionTestConfig.cs`
 - `src/CAL.Consumer.Api.Tests/appsettingsapitest.json`

### `CAL.Consumer.Jobs.Tests`

Use this test project when the target is the background queue processing pipeline.

Main characteristics:

- pushes messages into SQS
 - lets the running Jobs worker process them
 - validates logs, message deletion, and DLQ behavior
 - depends on the `consumer_jobs` container being available

Important setup files:

- `src/CAL.Consumer.Jobs.Tests/Shared/Config/TestFixture.cs`
 - `src/CAL.Consumer.Jobs.Tests/Shared/Config/ActionTestConfig.cs`
 - `src/CAL.Consumer.Jobs.Tests/Shared/Config/TestBeforeAfterCustom.cs`
 - `src/CAL.Consumer.Jobs.Tests/appsettings.json`

### `CAL.Consumer.Handler.Tests`

Use this test project when the target is a specific handler implementation and the full worker loop is not required.

Main characteristics:

- resolves handlers directly from DI
 - reuses the Jobs dependency graph
 - uses WireMock admin APIs to inspect outbound requests
 - captures logs in memory through `TestLoggerProvider`
 - disables parallelization for shared-state safety

Important setup files:

- `src/CAL.Consumer.Handler.Tests/SetupDI.cs`
 - `src/CAL.Consumer.Handler.Tests/Tests/Infrastructure/HandlerTestFixture.cs`
 - `src/CAL.Consumer.Handler.Tests/Tests/Infrastructure/HandlerTestCollection.cs`
 - `src/CAL.Consumer.Handler.Tests/Tests/Infrastructure/TestLoggerProvider.cs`
 - `src/CAL.Consumer.Handler.Tests/Tests/Handlers/HandlerTestBase.cs`
 - `src/CAL.Consumer.Handler.Tests/appsettings.json`

## High-Level Architecture

```text
 Api.Tests
 -> Http client
 -> running API container
 -> controllers + middleware + application services
 -> DynamoDB/SQS/external HTTP services
 -> assert response + docker logs

Jobs.Tests
 -> SQS test client
 -> running Jobs container
 -> queue polling + message processor + handlers
 -> DynamoDB/Redis/external HTTP services
 -> assert docker logs + queue/DLQ behavior

Handler.Tests
 -> test fixture
 -> production Jobs DI graph
 -> concrete handler resolved from DI
 -> repository + HTTP clients + security helper
 -> assert WireMock calls + in-memory logs
 ```

## Local Infrastructure Required

The tests depend on Docker services defined in `docker-compose.yml`.

Required services:

- `aws-proxy-local` on `http://localhost:4566`
 - `dynamodb` on `http://localhost:8000`
 - `moto` for AWS mocks
 - `wire_mock` on `http://localhost:40485`
 - `redis` on `localhost:6379`
 - `api` container named `consumer_api`
 - `jobs` container named `consumer_jobs`
 - optional init containers: `dynamodb-init`, `moto-init`

Relevant compose sections:

- AWS proxy: `docker-compose.yml:7-23`
 - API container: `docker-compose.yml:118-136`
 - Jobs container: `docker-compose.yml:140-159`
 - WireMock: `docker-compose.yml:164-170`
 - Redis: `docker-compose.yml:175-183`

## What Each Infrastructure Service Does

### `aws-proxy-local`

Acts as the single local AWS endpoint exposed to the application. In this repo it is used as the local replacement for AWS service URLs. Test code and containers point to `http://localhost:4566` or `http://aws-proxy-local:4566`.

### `dynamodb`

Provides DynamoDB Local. Test fixtures create and clear the `ConsumerMovesTable` so each scenario starts from a known state.

### `moto`

Provides mock AWS behavior for SQS and related services. The test projects push messages to queue URLs that resolve through the local AWS stack.

### `wire_mock`

Mocks downstream HTTP services such as:

- token endpoints
 - order status
 - DHL Link
 - consignment services
 - Google/Google Route
 - hub endpoints
 - vehicle lifecycle endpoints

This is the main assertion point for `Handler.Tests` and a supporting dependency for the other test projects.

### `redis`

Supports production behavior reused by Jobs and handler tests. Even when a test is not directly asserting Redis behavior, the DI graph may require it.

### `api` and `jobs`

These are the actual application containers under test for the API and Jobs integration suites. Their logs are read by the integration tests.

## Configuration Model

### API tests

`src/CAL.Consumer.Api.Tests/appsettingsapitest.json`

Key values:

- `ApiUrl` points to local nginx HTTPS proxy
 - AWS test endpoint points to `http://127.0.0.1:4566`
 - external service calls are redirected to WireMock on `http://localhost:40485`

### Jobs tests

`src/CAL.Consumer.Jobs.Tests/appsettings.json`

Key values:

- queue endpoint uses `http://127.0.0.1:4566/000000000000/MessageQueue.fifo`
 - Redis host is `redis`
 - most external dependencies use `http://localhost:40485`

### Handler tests

`src/CAL.Consumer.Handler.Tests/appsettings.json`

Key values:

- AWS service URL uses `http://localhost:4566`
 - SQS endpoint is redirected through WireMock for request inspection
 - downstream services point to WireMock
 - test fixture resets DynamoDB, WireMock, and captured logs before each scenario

## Detailed Setup Flow

### API test setup flow

`TestFixture` in `src/CAL.Consumer.Api.Tests/Shared/Config/TestFixture.cs` does the following:

1. builds configuration from `appsettingsapitest.json` and environment variables
 2. creates a `ServiceCollection`
 3. binds test settings with `ConfigureValidatableSetting<AppSettingsTestAPI>`
 4. constructs an internal `AppSettings` object for local AWS and mocked HTTP dependencies
 5. registers DynamoDB, SQS, HTTP clients, schedulers, Google services, and hub services
 6. builds a static service provider through `ServiceLocator.ServiceProvider`
 7. starts `DockerLogReader` for the `consumer_api` container
 8. clears existing logs, creates the DynamoDB table, and clears SQS messages

This means API tests are not spinning up the API process in-memory. They are preparing helper services locally while the real API container runs separately in Docker.

### Jobs test setup flow

`TestFixture` in `src/CAL.Consumer.Jobs.Tests/Shared/Config/TestFixture.cs` does the following:

1. builds configuration from `appsettings.json`
 2. binds `ServiceTestAppSettings`
 3. registers SQS, DynamoDB, HTTP clients, DynamoDB helpers, and test clients
 4. builds the shared service provider through `ServiceLocator.ServiceProvider`
 5. ensures the order table exists and clears previous database content
 6. starts `DockerLogReader` for the `consumer_jobs` container
 7. clears queue and DLQ messages

These tests then inject messages into SQS and wait for the running Jobs container to process them.

### Handler test setup flow

`HandlerTestFixture` in `src/CAL.Consumer.Handler.Tests/Tests/Infrastructure/HandlerTestFixture.cs` does the following:

1. builds configuration from `appsettings.json`
 2. creates a `TestLoggerProvider` for in-memory log capture
 3. builds DI through `SetupDI.SetupJobsInfrastructure(...)`
 4. reuses production Jobs registrations:
 - `services.SetupDatabases(...)`
 - `ApplicationServicesExtensions.AddApplicationServices(...)`
 - `services.AddHttpClients(...)`
 5. adds test logging overrides
 6. resolves `ISecurityServiceHelper` and warms required tokens
 7. resets state by clearing DynamoDB, resetting WireMock, and clearing logs

This is the cleanest reusable pattern because it exercises real handlers using the production DI graph without requiring the background queue polling loop.

## How Per-Test Data Setup Works

### API and Jobs tests

These projects use `TestBeforeAfterCustom` attributes to seed and remove data automatically.

Pattern:

1. the test `DisplayName` begins with a scenario ID such as `TC671435`
 2. `Before(...)` extracts that scenario ID
 3. `DataSeeding.DataAddFlow(...)` loads the matching seed data
 4. the test runs
 5. `After(...)` calls `DataSeeding.DataRemoveFlow(...)`

This makes each test case strongly tied to a scenario-based file name convention.

### Handler tests

These tests are more explicit.

They usually call either:

- `ResetAsync()`
 - `ResetAndSeedAsync(orderId)`

The shared `HandlerTestBase` handles:

- scenario ID extraction from `DisplayName`
 - seed file location
 - request file location
 - expected response/log file location

This makes the scenario contract very visible inside each test.

## How Assertions Work

## 1. API tests: response + docker logs

Typical API assertion flow:

1. build request model
 2. call a client wrapper such as `AppointmentOptionClient` or `VehicleEventClient`
 3. use `ActionTestConfig.RunActionAndViewLog(...)`
 4. get both:
 - HTTP response
 - filtered log lines from the `consumer_api` container
 5. assert status code, payload, and expected log fragments

`ActionTestConfig` for API tests works by:

- recording start time before the action
 - executing the HTTP call
 - reading correlation ID from the response header `X-CoxAuto-Correlation-Id`
 - scanning captured docker logs
 - filtering logs by time range first
 - extracting `TraceId`
 - narrowing the final log list to entries associated with the request trace

This is why API tests can assert both business result and the internal request flow.

Common assertion style:

- `Assert.Equal(HttpStatusCode.OK, response.StatusCode)`
 - `Assert.NotNull(response.Data)`
 - `Assert.Contains(logs, x => x.Contains("GenerateAppointmentOptionsRequestLogging"))`
 - `Assert.Contains(logs, x => x.Contains("GenerateAppointmentOptionsResponseLogging"))`

### Strengths

- validates full request pipeline
 - proves middleware and logging behavior
 - correlates one request to one trace

### Weaknesses

- brittle when log message text changes
 - depends on container log formatting and timing
 - slower than handler-level tests

## 2. Jobs tests: queue behavior + docker logs + DLQ checks

Typical Jobs assertion flow:

1. create or load a queue payload
 2. send it to SQS using the test `SqsClient`
 3. wait until the `consumer_jobs` container emits the expected log lines
 4. assert log content and sometimes queue or DLQ state

`ActionTestConfig` for Jobs tests provides log waiting helpers such as:

- `WaitForLogMessage(...)`
 - `WaitForLogMessageForSchedulingEvent(...)`
 - `GetLogMessageWithMsgId(...)`

The matching strategy is simpler than API tests:

- start from current UTC time
 - repeatedly read docker logs
 - filter logs newer than the start time
 - stop when a keyword has appeared the expected number of times
 - for message-specific flows, filter by the SQS message ID

Typical assertions include:

- message processed and deleted
 - message retained and eventually routed to DLQ
 - specific processing steps emitted expected log events

### Example outcomes asserted

- success path: `SUCCESSFULLY handle message ... deleting message`
 - retain path: no delete operation yet
 - failure path: repeated failure log entries and message present in DLQ

## 3. Handler tests: WireMock calls + in-memory logs + repository state

Typical handler assertion flow:

1. resolve a concrete handler from DI
 2. load the scenario request JSON
 3. create a synthetic `Amazon.SQS.Model.Message`
 4. invoke `handler.HandleMessage(message, token)` directly
 5. assert against:
 - repository state
 - WireMock recorded requests and responses
 - in-memory captured logs
 - whether a delete operation was sent for the SQS receipt handle

### WireMock-based assertions

`HandlerTestFixture` uses the WireMock admin endpoints:

- `/__admin/reset` to clear prior state
 - `/__admin/requests` to inspect request history

It exposes helpers such as:

- `FindWiremockRequestsAsync(...)`
 - `GetServeEventsAsync(...)`
 - `EnsureMessageDeletedAsync(...)`
 - `EnsureMessageRetainedAsync(...)`

These methods allow the test to verify:

- which downstream endpoint was called
 - how many times it was called
 - request body content
 - returned HTTP status code
 - whether the handler triggered SQS delete behavior

### Log assertions in handler tests

Handler tests do not scrape container logs.

Instead:

- `TestLoggerProvider` is registered into DI
 - every logger entry is captured in memory as `LogEntry`
 - `HandlerTestBase` compares actual captured logs against expected log files

This is more deterministic than docker log scraping because:

- it is synchronous to the test process
 - it does not depend on Docker output timing
 - it avoids parsing timestamps from container output

### Repository assertions

Some handler tests also query `IConsumerOrderRepository` directly to verify:

- created records
 - updated flags
 - record absence on failure paths

This gives a stronger assertion than logs alone.

## Assertion Strategy Comparison

| Test project | Main assertion style | Extra assertion style | Coupling |
 |---|---|---|---|
 | `CAL.Consumer.Api.Tests` | HTTP response | docker logs | high environment coupling |
 | `CAL.Consumer.Jobs.Tests` | queue outcome | docker logs + DLQ | high environment coupling |
 | `CAL.Consumer.Handler.Tests` | WireMock + repository state | in-memory logs | lower runtime coupling |

## Recommended Setup Steps for Another Project

### 1. Separate test layers

Create separate test projects for:

- API endpoint tests
 - background worker tests
 - direct handler tests

This keeps feedback loops clear:

- API tests validate the HTTP boundary
 - worker tests validate queue orchestration
 - handler tests validate business logic with lower cost

### 2. Standardize local dependencies

Use Docker Compose to provide:

- DynamoDB Local or equivalent datastore emulator
 - SQS/SNS emulator
 - WireMock for external HTTP dependencies
 - Redis if production code requires it
 - real application containers when full integration behavior is needed

### 3. Centralize fixture bootstrapping

Create one fixture per test style.

Each fixture should:

- load test configuration from `appsettings.json`
 - register production services through the normal composition root where possible
 - override only the parts needed for testing
 - reset shared state before each test

### 4. Prefer production DI reuse

The best reusable pattern in this repository is in `src/CAL.Consumer.Handler.Tests/SetupDI.cs`.

Instead of duplicating registrations, it reuses production startup methods:

- `services.SetupDatabases(...)`
 - `ApplicationServicesExtensions.AddApplicationServices(...)`
 - `services.AddHttpClients(...)`

For another project, follow the same rule:

- keep service registration in the main project
 - let tests reuse that registration
 - add only test-only logging or helpers in the fixture

### 5. Decide early how logs will be asserted

There are 2 patterns in this repo.

#### Pattern A: docker log assertions

Use when the target is the full running container and you need to verify request flow or background worker behavior.

Required pieces:

- a log collector like `DockerLogReader`
 - correlation or message ID based filtering
 - timeout-based polling
 - stable structured log messages

#### Pattern B: in-memory logger assertions

Use when tests execute code inside the test process through DI.

Required pieces:

- custom `ILoggerProvider`
 - deterministic log capture structure
 - helper methods to compare logs against expected data

For another project, prefer Pattern B whenever possible.

### 6. Use deterministic test data

Adopt a file-based convention:

- request payloads under `TestData/Requests`
 - seed data under `TestData/Seeds`
 - expected responses under `TestData/Expectations`
 - expected logs under `TestData/Expectations/.../Logs`

This repository already follows that pattern strongly in `CAL.Consumer.Handler.Tests`.

### 7. Add reset hooks

Before each test, reset:

- database state
 - queues and DLQ
 - WireMock request history
 - captured logs

Without this, tests become order-dependent.

### 8. Disable parallelization when shared state exists

If tests share DynamoDB tables, queues, or WireMock state, disable parallelization at the collection level.

Example in this repo:

- `src/CAL.Consumer.Handler.Tests/Tests/Infrastructure/HandlerTestCollection.cs`

## Suggested Implementation Plan for Another Project

### Option A: start with handler tests first

This is the recommended order.

1. expose production DI registration from the worker project
 2. create a `SetupDI` helper inside the handler test project
 3. create a fixture that:
 - builds DI
 - resets database state
 - resets WireMock
 - captures logs in memory
 4. add a test base class for:
 - request file loading
 - scenario ID extraction
 - expected log comparison
 - WireMock assertions
 5. add scenario-based test data files
 6. write direct handler tests

### Option B: add full worker integration tests next

1. keep the local AWS and queue infrastructure
 2. run the worker in Docker
 3. add a queue test client
 4. add docker log capture
 5. assert deletion, retention, and DLQ outcomes

### Option C: add API integration tests last

1. run the API container in Docker
 2. expose a local proxy or direct test URL
 3. create API clients inside the test project
 4. add correlation-ID log filtering
 5. assert response + logs together

## Suggested Folder Layout for Another Project

```text
 src/
 MyProject.Api/
 MyProject.Worker/
 MyProject.Shared/
 MyProject.Api.Tests/
 Shared/
 Tests/
 appsettings.json
 MyProject.Worker.Tests/
 Shared/
 Tests/
 appsettings.json
 MyProject.Handler.Tests/
 Tests/
 Infrastructure/
 Handlers/
 Shared/
 appsettings.json
 SolutionFolder/
 wiremock/
 mappings/
 dynamodb-init/
 moto-init/
 docker-compose.yml
 ```

## Running the Tests Locally

Start infrastructure first:

```powershell
 docker compose up -d --build
 ```

Run specific test projects:

```powershell
 dotnet test src/CAL.Consumer.Api.Tests/CAL.Consumer.Api.Tests.csproj
 dotnet test src/CAL.Consumer.Jobs.Tests/CAL.Consumer.Jobs.Tests.csproj
 dotnet test src/CAL.Consumer.Handler.Tests/CAL.Consumer.Handler.Tests.csproj
 ```

## Troubleshooting

### No logs found in API or Jobs tests

Check:

- `consumer_api` and `consumer_jobs` containers are running
 - container names match fixture expectations
 - the action produced a correlation ID or message ID
 - log timeout values are large enough
 - the test environment clock is not causing timestamp filter issues

### WireMock assertions fail in handler tests

Check:

- `wire_mock` is running on `http://localhost:40485`
 - downstream URLs in test `appsettings.json` point to WireMock
 - the test reset actually called `/__admin/reset`
 - the expected endpoint path matches the real outgoing request path

### Queue assertions fail

Check:

- queues were created by the init container
 - local AWS endpoints point to the correct proxy
 - the message contains required attributes such as message type
 - the handler or worker had permission/configuration to process the message type

### Seed data issues

Check:

- scenario ID in `DisplayName` matches the file name
 - seed files and request files use the expected folder layout
 - cleanup is not deleting data needed by the current test

## Practical Recommendation

If another project only needs one reusable pattern, copy the `CAL.Consumer.Handler.Tests` approach first.

Reason:

- it is faster than full service tests
 - it reuses production DI cleanly
 - it avoids container log scraping
 - it verifies external calls through WireMock in a deterministic way
 - it still allows repository-state and log assertions

Then add API and worker integration tests only for full pipeline coverage.

## Checklist for Reuse in Another Project

- [ ] create Docker Compose for local dependencies
 - [ ] create WireMock mappings for downstream services
 - [ ] expose production DI registration methods for reuse
 - [ ] create one fixture per test style
 - [ ] add reset logic for DB, queues, WireMock, and logs
 - [ ] decide whether each suite uses docker logs or in-memory log capture
 - [ ] store requests, seeds, responses, and expected logs in files
 - [ ] disable parallelization if shared state exists
 - [ ] keep handler tests as the primary fast integration layer
