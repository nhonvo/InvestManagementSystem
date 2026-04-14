# User Authentication Flow

> How users register, log in, refresh tokens, and access protected resources.

## JWT Token Lifecycle

Access token TTL = **15 minutes**. Refresh token TTL = **7 days** (stored as `httpOnly; Secure; SameSite=Strict` cookie).

```mermaid
sequenceDiagram
    participant User
    participant UI as Next.js UI
    participant API as InventoryAlert.Api
    participant DB as PostgreSQL

    User->>UI: Fills credentials (username + password)
    UI->>API: POST /api/v1/auth/login
    API->>DB: SELECT User WHERE Username = X
    DB-->>API: User entity (PasswordHash)
    API->>API: BCrypt.Verify(password, hash)
    API-->>UI: 200 { accessToken (JWT), expiresAt }
    Note over API,UI: Refresh token set as httpOnly cookie
    UI->>API: GET /api/v1/portfolio/positions\n(Authorization: Bearer <token>)
    API->>API: Validate JWT (signature + expiry + issuer + audience)
    API-->>UI: 200 OK + PagedResult<PortfolioPositionResponse>
```

---

## Token Refresh Flow

```mermaid
sequenceDiagram
    participant UI as Next.js UI
    participant API as InventoryAlert.Api

    Note over UI: Access token expired (15 min)
    UI->>API: POST /api/v1/auth/refresh
    Note over UI,API: Refresh token read from httpOnly cookie (no body)
    API->>API: Validate refresh token (JWT signature + claims)
    API-->>UI: 200 New { accessToken, expiresAt }
    Note over API,UI: New refresh token set in cookie (rotated)
```

---

## Registration Flow

```mermaid
sequenceDiagram
    participant User
    participant UI as Next.js UI
    participant API as InventoryAlert.Api
    participant DB as PostgreSQL

    User->>UI: Fills registration form (username, email, password)
    UI->>API: POST /api/v1/auth/register
    API->>DB: SELECT User WHERE Username = X OR Email = X
    alt Username/Email already exists
        API-->>UI: 409 Conflict
    else New User
        API->>API: BCrypt.HashPassword(password)
        API->>DB: INSERT User { Username, Email, PasswordHash, Role="User" }
        API-->>UI: 200 { Message: "Registration successful." }
    end
```

---

## JWT Token Claims

```json
{
  "sub": "00000000-0000-0000-0000-000000000001",
  "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "admin",
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "Admin",
  "jti": "unique-token-id",
  "iss": "InventoryAlert.Api",
  "aud": "InventoryAlert.UI",
  "exp": 1713000000
}
```

| Claim | Purpose |
|---|---|
| `sub` | User ID (Guid) — used by all services to scope data access |
| `name` | Username — displayed in UI |
| `role` | `User` or `Admin` — controls endpoint authorization |
| `exp` | Token expiry — 15 minutes from issuance |
| `iss` / `aud` | Validated on every request to prevent token reuse |

---

## Authorization Levels

| Endpoint | Required |
|---|---|
| `POST /auth/login`, `POST /auth/register`, `POST /auth/refresh` | `[Public]` — no token needed |
| All other endpoints | `[Authorize]` — valid JWT required |
| `POST /stocks/sync`, `GET/POST /events/*` | `[Authorize(Roles = "Admin")]` |
| `GET /market/status` | `[AllowAnonymous]` — explicitly public |

---

## Security Considerations

- Passwords are hashed with **BCrypt** (default work factor 11).
- Access token delivered in JSON body; **refresh token in `httpOnly` cookie** — not accessible to JavaScript.
- Refresh tokens are **single-use** (rotated on each refresh call).
- All sensitive config (`Jwt:Key`, `Database:ConnectionString`, `Finnhub:ApiKey`) lives in `appsettings.*.json` which is **gitignored**. Only `appsettings.Example.json` is committed.
- Login endpoint is **429 rate-limited** to prevent brute-force attacks.
