# User Authentication Flow

> How users register, log in, and access protected resources.

## JWT Token Lifecycle

```mermaid
sequenceDiagram
    participant User
    participant UI as Next.js UI
    participant API as InventoryAlert.Api

    User->>UI: Fills in credentials
    UI->>API: POST /auth/login
    API->>API: Validate credentials, hash compare
    API-->>UI: { accessToken, refreshToken }
    UI->>UI: Stores token (HttpOnly Cookie)
    UI->>API: GET /products (Authorization: Bearer <token>)
    API->>API: Validate JWT signature + expiry
    API-->>UI: 200 OK + data
```

## Authorization
- **User Role**: Full access to own watchlists and alert rules.
- **Admin Role**: Access to all users' data and Hangfire Dashboard.
