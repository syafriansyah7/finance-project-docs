# Test Plan

## 1. Purpose

Verify correctness of transactions, offline behavior, synchronization, API behavior, database integrity, security baseline, deployment, and recovery.

## 2. Test levels

### Unit tests

Test pure business rules:

- income calculation;
- expense calculation;
- transfer rules;
- budget calculations;
- validation;
- sync state transitions.

### Integration tests

Test:

- ASP.NET Core + PostgreSQL;
- authentication;
- transaction API;
- sync API;
- EF Core migrations;
- idempotency.

### Mobile tests

Test SQLite repositories and sync orchestration separately from UI components.

### End-to-end tests

Test a complete path:

```text
Mobile -> SQLite -> API -> PostgreSQL -> Web Dashboard
```

## 3. Critical scenarios

| ID | Scenario | Expected result |
|---|---|---|
| T01 | Create expense online | Saved locally and server-side |
| T02 | Create expense offline | Saved immediately to SQLite as pending |
| T03 | Close/reopen app offline | Pending transaction remains |
| T04 | Network returns | Pending transaction syncs |
| T05 | Same sync request twice | No duplicate transaction |
| T06 | Network drops during sync | Item remains pending and retries |
| T07 | Permanent validation error | Item becomes failed and is actionable |
| T08 | Create transfer | Source decreases, destination increases |
| T09 | Transfer reporting | Not counted as income/expense |
| T10 | Dashboard refresh | Values match server database |
| T11 | Server restart | Data remains intact and service recovers |
| T12 | PostgreSQL backup | Backup can be created |
| T13 | PostgreSQL restore | Restored database matches expected data |
| T14 | HTTPS API | Requests are encrypted |
| T15 | Public PostgreSQL probe | Database is unreachable from the public internet |

## 4. Data integrity tests

Verify:

- positive amounts where required;
- valid account references;
- valid category references;
- transfer source != destination;
- archived entities follow business rules;
- UUID uniqueness;
- timestamps are coherent.

## 5. Offline testing matrix

Test the app under:

- Wi-Fi on;
- Wi-Fi off;
- mobile data off;
- airplane mode;
- intermittent connectivity;
- server unavailable while internet is available.

## 6. Performance expectations

This is a personal-scale application. Tests should focus on responsiveness and correctness rather than synthetic enterprise-scale throughput.

## 7. Release gate

A release is not ready when:

- offline transaction creation loses data;
- duplicate sync can create duplicate financial records;
- restore has not been verified;
- production HTTPS is missing;
- PostgreSQL is publicly exposed;
- critical business-rule tests fail.
