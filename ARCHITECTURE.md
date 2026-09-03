# Architecture

## 1. Purpose

This document defines the technical architecture for the personal finance application.

The system is intentionally designed for a single user with one primary mobile device. The laptop is a monitoring and dashboard device rather than the primary transaction-entry device.

## 2. Architecture decisions

| Area | Decision |
|---|---|
| Mobile | .NET MAUI + Blazor Hybrid |
| Local mobile DB | SQLite |
| Backend | ASP.NET Core / C# |
| Server DB | PostgreSQL |
| Web dashboard | Blazor |
| Mobile offline | SQLite + sync queue |
| Cloud | Oracle Cloud Always Free target |
| Containers | Docker + Docker Compose |
| Laptop role | Monitoring / dashboard |
| Google Sheets | Optional export/reporting |
| Backup | Required |
| Security | Basic, correct, HTTPS-first |

## 3. Logical architecture

```text
                         Internet
                            |
                    HTTPS / Reverse Proxy
                            |
                     ASP.NET Core API
                            |
                 +----------+----------+
                 |                     |
            PostgreSQL            Background Jobs
                 |                     |
                 +----------+----------+
                            |
                    Blazor Web App
                            |
                         Laptop

Android phone
     |
.NET MAUI + Blazor Hybrid
     |
    SQLite
     |
 Sync Queue
     |
     +---- offline ----> local only
     |
     +---- online -----> ASP.NET Core API
```

## 4. Responsibilities

### Mobile application

The mobile application is responsible for:

- transaction entry;
- transaction viewing;
- local persistence;
- offline operation;
- maintaining a pending-sync queue;
- synchronizing local changes to the API;
- presenting sync status and errors clearly.

The mobile application must not embed server secrets.

### Backend API

The backend is responsible for:

- authentication and authorization;
- validation;
- authoritative persistence;
- transaction rules;
- reporting queries;
- synchronization endpoints;
- idempotency;
- audit-friendly timestamps.

### PostgreSQL

PostgreSQL is the canonical server-side database.

It stores normalized transactional data. Derived balances and reports should be calculated from transaction/account data rather than copied as mutable state wherever practical.

### Web dashboard

The web dashboard is responsible for:

- current balances;
- monthly income/expense summary;
- category breakdown;
- budget progress;
- recent transactions;
- filters and reporting.

The laptop UI does not need the same quick-entry workflow as the phone.

### Google Sheets

Google Sheets is optional. It may receive exports or reporting data from PostgreSQL, but it must not be treated as the application database.

## 5. Offline-first behavior

### Write path while offline

```text
User enters transaction
        |
        v
Validate locally
        |
        v
Write to SQLite
        |
        v
Create sync_queue item
        |
        v
Show transaction immediately
```

### Sync path when online

```text
Find pending queue items
        |
        v
Send idempotent API request
        |
   +----+----+
   |         |
 success   failure
   |         |
   v         v
mark       retain + retry
synced
```

## 6. Single-device simplification

Only one mobile device is in scope for v1. Therefore:

- no cross-device merge protocol is required;
- no CRDT is required;
- no multi-master database is required;
- server-side idempotency is still required;
- the device can safely use a monotonically tracked sync cursor/version supplied by the server.

## 7. Transaction semantics

Transactions are the fundamental money movement records.

Supported v1 transaction types:

- `Income`
- `Expense`
- `Transfer`

A transfer moves money between accounts and must not count as income or expense in reports.

The application should calculate balances from transaction data rather than synchronizing a mutable balance field between devices.

## 8. Deployment architecture

Target VPS:

```text
Oracle Cloud Always Free
  |
  Docker
    |
    +-- reverse proxy
    +-- finance API
    +-- PostgreSQL
    +-- worker/background jobs
```

PostgreSQL must not be directly exposed to the public internet.

## 9. Network boundaries

Publicly reachable:

- HTTPS application/API endpoint.

Privately reachable:

- PostgreSQL.
- Internal container network.

Administrative access:

- SSH using keys.

## 10. Reliability

The design assumes the VPS can fail. Therefore:

- local phone data remains useful while offline;
- pending transactions must survive temporary network failures;
- PostgreSQL backups are mandatory;
- restore procedures must be documented and periodically tested.

## 11. Resource philosophy

The project should remain small:

- one backend application;
- one PostgreSQL database;
- one worker process/job mechanism if required;
- one reverse proxy;
- one Docker Compose deployment.

No microservice split is planned for v1.
