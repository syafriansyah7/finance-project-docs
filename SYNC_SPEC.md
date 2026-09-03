# Synchronization Specification

## 1. Scope

This document defines synchronization between the single primary mobile device and the server.

The project has **one primary phone only in v1**. There is no cross-device conflict-resolution protocol.

## 2. Source of truth model

While offline:

> SQLite is the operational local source of truth for the mobile device.

After successful server synchronization:

> PostgreSQL is the canonical server source of truth.

The mobile app must never overwrite a correct server transaction merely because its locally derived balance is different.

## 3. Sync architecture

```text
.NET MAUI + Blazor Hybrid
        |
      SQLite
        |
    sync_queue
        |
   HTTPS API
        |
 ASP.NET Core
        |
 PostgreSQL
```

## 4. Local transaction lifecycle

Every locally created or modified sync-sensitive entity follows:

```text
Local change
    |
    v
Pending
    |
    +---- failure ----> Pending / RetryScheduled
    |
    +---- success ----> Synced
```

Deletion should use a tombstone/soft-delete model until the server confirms synchronization.

## 5. Queue fields

A minimal queue record should include:

- queue item ID;
- entity type;
- entity ID;
- operation type;
- serialized payload or reference to local row;
- attempt count;
- last attempt timestamp;
- next retry timestamp;
- last error code/message where safe;
- created timestamp.

## 6. Idempotency

Every sync operation must carry a unique operation ID generated on the client.

Example:

```json
{
  "operationId": "550e8400-e29b-41d4-a716-446655440000",
  "entityId": "...",
  "operation": "create",
  "payload": {}
}
```

The server must ensure that retrying the same operation ID does not create duplicate financial records.

## 7. Push strategy

The mobile app should push pending queue items in deterministic order, normally oldest first.

Pseudo-flow:

```text
while pending items exist:
    take next pending item
    send to API
    if success:
        mark synced
    else if transient failure:
        keep pending and schedule retry
    else if permanent validation failure:
        mark failed and show user action
```

## 8. Pull strategy

Because there is only one primary phone, the server does not need a complex bidirectional merge engine in v1.

After pushes, the mobile app may request server changes using a cursor/version to repair missed updates, refresh deleted records, and ensure local consistency.

Recommended concept:

```text
GET /api/v1/sync/pull?cursor=<cursor>
```

The exact cursor implementation is an implementation decision.

## 9. Retry policy

Transient failures include:

- no network;
- timeout;
- temporary 5xx responses;
- DNS/connectivity failures.

Use bounded exponential backoff with jitter. Example policy:

```text
attempt 1: 5s
attempt 2: 15s
attempt 3: 60s
attempt 4: 5m
attempt 5+: capped retry interval
```

The exact values are configurable.

## 10. Permanent failures

Examples:

- invalid category ID;
- invalid account ID;
- malformed payload;
- authorization revoked;
- server rejected business rule.

Do not retry a permanent validation error forever. Mark the item failed and provide a recovery path.

## 11. Transaction conflict model

There is no multi-device conflict model in v1.

However, the server remains authoritative for validation. For example:

- archived account cannot receive a new transaction if business rules prohibit it;
- deleted category references must be rejected or migrated explicitly;
- duplicate operation IDs must be safely ignored/replayed.

## 12. Offline read behavior

The mobile application should read transaction lists, accounts, categories, and other frequently needed data from SQLite first.

Server refresh is asynchronous.

This avoids making the user wait for a network round trip.

## 13. Sync status UX

The application should display a compact status such as:

```text
Synced
3 transactions waiting to sync
Sync failed - tap to retry
```

Do not expose raw exception stacks to the user.

## 14. Required tests

At minimum:

1. Create transaction offline and close the app.
2. Reopen offline and confirm it remains visible.
3. Restore network and verify one successful server record.
4. Force the same request to be sent twice and verify no duplicate.
5. Fail the network during upload and verify retry.
6. Restart the server during sync and verify recovery.
7. Verify failed permanent validation is surfaced without infinite retry.
