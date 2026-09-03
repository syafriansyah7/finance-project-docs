# API Specification

## 1. Scope

This document defines the initial HTTP API contract between the .NET MAUI/Blazor Hybrid mobile application, the Blazor web dashboard, and the ASP.NET Core backend.

The API is versioned under `/api/v1`.

## 2. Conventions

- Protocol: HTTPS
- Format: JSON
- Character set: UTF-8
- Date/time: ISO 8601 / UTC on the server
- IDs: UUID
- Money: integer minor units where practical; for IDR, this means whole rupiah values.
- Authentication: bearer access token after login
- Error responses: structured JSON

Example error:

```json
{
  "error": {
    "code": "validation_error",
    "message": "Amount must be greater than zero",
    "details": {
      "amount": ["Must be greater than zero"]
    },
    "traceId": "..."
  }
}
```

## 3. Health

### `GET /health`

Returns service health information suitable for monitoring.

Response `200`:

```json
{
  "status": "ok"
}
```

## 4. Authentication

### `POST /api/v1/auth/login`

Authenticates the user.

Request:

```json
{
  "email": "user@example.com",
  "password": "..."
}
```

Response `200`:

```json
{
  "accessToken": "...",
  "expiresAt": "2026-09-02T00:00:00Z"
}
```

> Exact refresh-token strategy remains an implementation decision and should be finalized before production.

### `POST /api/v1/auth/refresh`

Refreshes an authenticated session when refresh tokens are enabled.

## 5. Accounts

### `GET /api/v1/accounts`

Returns accounts available to the authenticated user.

### `POST /api/v1/accounts`

Creates an account.

Request:

```json
{
  "name": "Cash",
  "type": "Cash",
  "currency": "IDR"
}
```

### `GET /api/v1/accounts/{id}`

Returns account detail and current derived balance.

### `PUT /api/v1/accounts/{id}`

Updates editable account metadata.

### `DELETE /api/v1/accounts/{id}`

Soft-deletes or archives an account if the account is not prohibited from deletion by business rules.

## 6. Categories

### `GET /api/v1/categories`

Returns transaction categories.

### `POST /api/v1/categories`

Creates a category.

Request:

```json
{
  "name": "Food",
  "kind": "Expense"
}
```

### `PUT /api/v1/categories/{id}`

Updates category metadata.

### `DELETE /api/v1/categories/{id}`

Archives a category rather than destroying historical references.

## 7. Transactions

### `GET /api/v1/transactions`

Returns a paginated, filterable transaction list.

Query parameters:

- `from`
- `to`
- `accountId`
- `categoryId`
- `type`
- `page`
- `pageSize`

### `GET /api/v1/transactions/{id}`

Returns one transaction.

### `POST /api/v1/transactions`

Creates a transaction.

Request:

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "type": "Expense",
  "accountId": "...",
  "categoryId": "...",
  "amount": 25000,
  "currency": "IDR",
  "description": "Nasi padang",
  "transactionDate": "2026-09-01T15:00:00Z"
}
```

The client-generated UUID is important for offline-first idempotency.

### `PUT /api/v1/transactions/{id}`

Updates a transaction using the same stable UUID.

### `DELETE /api/v1/transactions/{id}`

Deletes/voids a transaction according to the chosen soft-delete policy.

## 8. Sync

### `GET /api/v1/sync/pull?cursor={cursor}`

Returns server-side changes after a supplied cursor/version.

Example:

```json
{
  "cursor": "12345",
  "changes": [
    {
      "entity": "category",
      "operation": "upsert",
      "id": "...",
      "data": { }
    }
  ]
}
```

Because v1 has one mobile device, pull sync is primarily for refreshing the phone from the server after reinstall/recovery.

### `POST /api/v1/sync/push`

Uploads one or more pending local changes.

Request:

```json
{
  "items": [
    {
      "operationId": "...",
      "entity": "transaction",
      "entityId": "...",
      "operation": "create",
      "clientUpdatedAt": "2026-09-01T15:00:00Z",
      "payload": { }
    }
  ]
}
```

Response:

```json
{
  "results": [
    {
      "operationId": "...",
      "status": "accepted",
      "serverVersion": 12346
    }
  ],
  "nextCursor": "12346"
}
```

The server must make a successfully processed `operationId` idempotent so that retries do not create duplicate transactions.

## 9. Budgets

### `GET /api/v1/budgets?month=2026-09`

Returns monthly budget information.

### `POST /api/v1/budgets`

Creates or replaces a category budget for a month.

Request:

```json
{
  "month": "2026-09",
  "categoryId": "...",
  "amount": 1500000,
  "currency": "IDR"
}
```

### `PUT /api/v1/budgets/{id}`

Updates a budget.

### `DELETE /api/v1/budgets/{id}`

Removes/archives a budget.

## 10. Dashboard

### `GET /api/v1/dashboard/summary?month=2026-09`

Returns aggregated values for the dashboard.

Example:

```json
{
  "month": "2026-09",
  "income": 10000000,
  "expense": 3250000,
  "net": 6750000,
  "accounts": [
    {
      "id": "...",
      "name": "Cash",
      "balance": 500000
    }
  ],
  "categoryExpenses": [
    {
      "categoryId": "...",
      "categoryName": "Food",
      "amount": 1200000
    }
  ]
}
```

## 11. API rules

1. Authenticated endpoints require authentication.
2. The API validates all input; client validation is not trusted.
3. Amounts must be positive magnitudes; transaction `type` determines direction.
4. Currency must be explicit even if only `IDR` is supported in v1.
5. IDs must be stable UUIDs.
6. Sync requests must be retry-safe.
7. Server timestamps are authoritative for persistence metadata.
8. Historical transaction records should not be silently reinterpreted when categories are renamed.

## 12. HTTP status codes

- `200` — successful read/update
- `201` — created
- `204` — successful delete/archive with no body
- `400` — malformed request
- `401` — unauthenticated
- `403` — authenticated but forbidden
- `404` — resource not found
- `409` — semantic/idempotency conflict
- `422` — validation/business rule failure
- `429` — rate limited
- `500` — unexpected server error

## 13. Future extension

The API may later support receipt uploads, recurring transactions, exports, or additional analytics. These should be additive and version-safe.
