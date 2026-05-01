---
description: Review of current API error handling and response format, plus recommendations to standardize status codes and error messages.
type: reference
status: draft
version: 1.0
tags: [api, middleware, error-handling, standardization, inventoryalert]
last_updated: 2026-04-26
---

# Error Handling & Response Standardization (Review + Recommendations)

Scope: `InventoryManagementSystem/InventoryAlert.Api` error handling (middleware + validation filter) and the **shape of error responses** returned to clients.

This is a documentation-only review (no code changes included).

---

## 1) Current Behavior (Observed in Code)

### 1.1 Error response DTOs

Defined in `InventoryManagementSystem/InventoryAlert.Api/Models/Error.cs`:

- `ErrorResponse`
  - `Errors: IEnumerable<Error>`
  - `ErrorId: Guid` (generated per response)
- `Error`
  - `Code: string?`
  - `Message: string?`
  - `Property: string?`

### 1.2 Model validation (400 Bad Request)

MVC setup in `InventoryManagementSystem/InventoryAlert.Api/Extensions/MvcExtension.cs`:

- `SuppressModelStateInvalidFilter = true`
- Adds `ValidateModelFilter` globally
- Enables FluentValidation auto-validation + interceptor

Validation responses are emitted by:

- `InventoryManagementSystem/InventoryAlert.Api/Filters/ValidateModelFilter.cs`
  - Returns `BadRequestObjectResult(new ErrorResponse(errors))`
  - Each error uses:
    - `Code = "InventoryAlert.BAD_REQUEST"`
    - `Property = <field name or "General">`
    - `Message = <ModelState error message>`

### 1.3 Unhandled exceptions (GlobalExceptionMiddleware)

Middleware: `InventoryManagementSystem/InventoryAlert.Api/Middleware/GlobalExceptionMiddleware.cs`

Behavior:

- Catches exceptions, logs error (including `X-Correlation-Id` when present).
- Maps exceptions to HTTP status codes and returns a JSON error payload:
  - Default: `500 Internal Server Error` with code `InventoryAlert.GENERAL_ERROR`
  - `UserFriendlyException` → status mapped by `ErrorCode` (404/409/400/401/403/422/500)
  - `KeyNotFoundException` or `NotFoundException` → 404 with code `InventoryAlert.NOT_FOUND`
  - `FluentValidation.ValidationException` or domain `ValidationException` → 400 with code `InventoryAlert.BAD_REQUEST`
  - `ArgumentException` → 400 with code `InventoryAlert.BAD_REQUEST`
  - `UnauthorizedAccessException` → 401 with code `InventoryAlert.UNAUTHORIZED`
- Writes response using `await context.Response.WriteAsync(errorResponse.ToString())`

### 1.4 Middleware ordering (important)

Pipeline order in `InventoryManagementSystem/InventoryAlert.Api/Program.cs`:

1) `CorrelationIdMiddleware`
2) `GlobalExceptionMiddleware`
3) `PerformanceMiddleware`

Impact:

- When downstream throws, `PerformanceMiddleware` does not log a completion entry (because it does not catch/finally), and exception is handled “above” it by `GlobalExceptionMiddleware`.
- Result: failed requests may have an error log but no standard “request completed” log entry.

---

## 2) Inconsistencies / Risks

### 2.1 Error code format is inconsistent

Two naming paths exist:

- `ErrorRespondCode` enum values (ex: `NOT_FOUND`, `BAD_REQUEST`) produce codes like:
  - `InventoryAlert.NOT_FOUND`
- `UserFriendlyException.ErrorCode` enum values (ex: `NotFound`) are currently serialized via `.ToUpper()` which produces:
  - `InventoryAlert.NOTFOUND` (missing underscore)

This makes client-side handling and Seq searching harder because codes are not stable.

### 2.2 Serialization is inconsistent (middleware vs MVC)

- Model validation uses `BadRequestObjectResult(new ErrorResponse(...))` and will be serialized by ASP.NET Core’s configured JSON options.
- `GlobalExceptionMiddleware` manually serializes via `ErrorResponse.ToString()` with its own `JsonSerializerOptions` (camelCase only).

Risk:

- If global JSON options change (naming, null handling, converters), middleware errors won’t match controller/validation errors.

### 2.3 CorrelationId is not part of the JSON response body

The response header `X-Correlation-Id` exists (good), but the JSON payload doesn’t include it.

Operationally, it is easier when the client can log/store a single `correlationId` field from the body for every error response.

### 2.4 Validation interceptor behavior looks incomplete

`InventoryManagementSystem/InventoryAlert.Api/Validations/ValidatorInterceptor.cs` builds an `ErrorResponse` but does not return it and does not attach structured validation errors to the response directly.

Additionally, the interceptor clears and replaces ModelState errors under the `FluentValidation` key, which may cause:

- Missing/empty `ErrorMessage` values in `ValidateModelFilter` depending on how the ModelState errors are populated.

This is a “review risk” item: validate actual runtime payloads for validation errors to ensure field messages are not lost.

### 2.5 Some status semantics are likely underspecified

Examples:

- A “symbol not recognized” case currently throws `KeyNotFoundException` in services → becomes 404.
  - Many APIs would use 404 for missing resources, but “invalid symbol input” might be better as 400/422 depending on business semantics.
- Domain/business-rule failures are not consistently represented as 409 vs 422 vs 400.

---

## 3) Recommended Standard (Stable, Searchable, Client-Friendly)

Pick one of these two approaches and apply it consistently:

### Option A (minimal change): keep `ErrorResponse`, standardize fields + mapping

**Response schema (recommended)**

```json
{
  "errorId": "guid",
  "correlationId": "string",
  "errors": [
    { "code": "InventoryAlert.BAD_REQUEST", "message": "…", "property": "TickerSymbol" }
  ]
}
```

Rules:

- Always return `ErrorResponse` for non-2xx.
- Always include `correlationId` in the JSON body (and keep the header).
- Keep `errorId` as a server-generated unique id (good for support tickets).
- Ensure `code` values are stable and come from **one enum/source of truth** (recommend using `ErrorRespondCode` style with underscores).

### Option B (more standard): adopt RFC 7807 Problem Details

Use `ProblemDetails` for the main envelope and include your current `errors[]` in extensions.

This aligns with many .NET clients and tooling, but is a bigger change and may not be necessary if `ErrorResponse` is already established.

---

## 4) Status Code & Error Code Mapping (Proposed)

This table is a suggested baseline:

| HTTP | Code | When |
|---:|---|---|
| 400 | `InventoryAlert.BAD_REQUEST` | invalid input shape, parsing, validation failures |
| 401 | `InventoryAlert.UNAUTHORIZED` | missing/invalid auth, invalid refresh token |
| 403 | `InventoryAlert.FORBIDDEN` | authenticated but lacks permission |
| 404 | `InventoryAlert.NOT_FOUND` | resource id not found (rule id, notification id, etc.) |
| 409 | `InventoryAlert.CONFLICT` | version conflict / uniqueness / state conflict |
| 422 | `InventoryAlert.UNPROCESSABLE_ENTITY` | business-rule violation when input is syntactically valid |
| 500 | `InventoryAlert.INTERNAL_ERROR` (or `GENERAL_ERROR`) | unexpected server failures |

Recommendation:

- Remove duplicate “500 codes” (`INTERNAL_ERROR` vs `GENERAL_ERROR`) or define strict meaning:
  - `INTERNAL_ERROR`: known internal failure classification
  - `GENERAL_ERROR`: fallback for truly unknown exceptions

---

## 5) Middleware / Filter Recommendations

### 5.1 Ensure failed requests still produce a standard “request completed” log

If you want every request (success/failure) logged once, you need either:

- a request logging middleware that logs in `finally`, or
- move the request timing middleware *outside* the exception middleware and catch exceptions inside the timing middleware (still rethrow).

### 5.2 Standardize response serialization in middleware

Do not manually serialize `ErrorResponse` using a local `JsonSerializerOptions`.

Instead, emit an object result using the app’s configured JSON options (so errors from middleware match errors from controllers/filters).

### 5.3 Fix error code formatting once

Pick one canonical error code style:

- `InventoryAlert.NOT_FOUND` (underscore-separated), recommended because you already have `ErrorRespondCode`.

Then ensure:

- `UserFriendlyException` uses the same mapping, not `.ToUpper()` of enum names.

### 5.4 Validation errors: keep field-level details

Ensure validation responses always contain:

- field name (`property`)
- message (`message`)
- stable code (`InventoryAlert.BAD_REQUEST`)

Avoid injecting exceptions into ModelState if it causes empty messages.

---

## 6) Quick Checklist (Acceptance Criteria)

- All error responses follow a single schema (same fields across validation + exceptions).
- Error codes have a stable, consistent format (no `NOTFOUND` vs `NOT_FOUND`).
- `X-Correlation-Id` exists and `correlationId` exists in the JSON body.
- Serialization is consistent across middleware and MVC.
- Error responses never leak sensitive content (tokens, secrets, stack traces) in Production.

