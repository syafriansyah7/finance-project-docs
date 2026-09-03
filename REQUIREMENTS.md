# Requirements Specification

## 1. Purpose

This document defines the product requirements for the personal finance application. It is intentionally simple and user-centered because the project is for one person and is not intended to be a commercial accounting system.

## 2. Product vision

Create a private personal finance application where the **phone is the primary transaction-entry device**, transaction entry continues when the phone has no internet connection, the VPS stores the canonical synchronized data, and a laptop provides a clear dashboard for monitoring and understanding finances.

The application should minimize friction when recording everyday transactions and avoid forcing the user to understand formal accounting concepts.

## 3. Users

### Primary user

- One private user.
- One primary Android phone in v1.
- Laptop is a monitoring/dashboard device.

### Out of scope

- Multi-user households.
- Multiple synchronized phones.
- Public SaaS users.
- Complex permissions/roles.

## 4. Functional requirements

### 4.1 Accounts

The user must be able to:

- create an account;
- edit an account;
- archive an account;
- view its calculated balance;
- identify its currency.

Initial examples:

- Cash
- Bank
- E-Wallet

### 4.2 Categories

The user must be able to:

- create a category;
- edit a category;
- archive a category;
- assign categories to income and expense transactions.

Suggested starter categories:

- Food
- Transport
- Bills
- Shopping
- Entertainment
- Health
- Salary / Income
- Other

The exact defaults remain a product decision.

### 4.3 Transactions

The phone must support fast recording of:

- Income
- Expense
- Transfer

Minimum fields:

- amount;
- account;
- transaction type;
- category when applicable;
- date/time;
- optional description/note.

A transfer must have a source account and destination account and must not be included as income or expense in reports.

### 4.4 Offline operation

The phone must allow core transaction creation while offline.

Offline transaction flow:

```text
Create transaction
        -> validate locally
        -> save to SQLite
        -> mark pending
        -> show immediately in UI
```

### 4.5 Synchronization

When network connectivity returns, pending local changes must be uploaded automatically or through a clear manual retry action.

The server must process duplicate sync attempts safely.

### 4.6 Dashboard

The laptop dashboard must show at minimum:

- current account balances;
- total income for selected period;
- total expenses for selected period;
- net change for selected period;
- spending by category;
- recent transactions;
- budget progress when budgets are enabled.

### 4.7 Budgeting

The first budgeting model should be simple:

```text
Category + Period + Limit
```

The system should calculate:

```text
Spent
Remaining
Percentage used
```

Advanced envelope/double-entry budgeting is not required for v1.

### 4.8 Reporting/export

The application must support reliable data export. CSV export is required at the product level even if the first UI is minimal.

Google Sheets is optional and must never be required for normal application operation.

## 5. Non-functional requirements

### Reliability

- Pending offline transactions must survive app restarts.
- Pending sync must retry after temporary failures.
- Database backups are required.

### Performance

- Local transaction creation should feel immediate.
- Dashboard queries should remain responsive for a personal-scale dataset.
- No premature distributed architecture.

### Security

Use a basic but correct security baseline. Do not expose PostgreSQL publicly. Use HTTPS for production API traffic.

### Portability

The project must remain deployable to another Linux VPS without rewriting business logic.

### Cost

Operational target is **Rp0**. Paid services must not be required by the architecture. Free-tier availability may change and must be verified before production deployment.

## 6. UX requirements

### Mobile priority

Transaction capture should take only a few interactions.

Preferred flow:

```text
Amount
Category
Account
Save
```

Optional fields should not block the save operation.

### Clear offline status

The mobile UI must make it understandable whether data is:

- saved locally;
- waiting to sync;
- synchronized;
- failed and awaiting retry.

## 7. Data integrity requirements

- Use UUID/GUID identifiers for sync-sensitive entities created locally.
- Do not manually synchronize mutable balances.
- Derive balances from transactions.
- Transfers must preserve accounting consistency between accounts.
- Server requests that can be retried must be idempotent.

## 8. Future candidates

These are not v1 requirements:

- receipt photos/OCR;
- recurring transactions;
- CSV bank import;
- advanced analytics;
- notifications;
- multiple devices;
- bank API integrations;
- iOS support if not prioritized after Android v1.

## 9. Open product decisions

The following remain intentionally open:

- final app name;
- exact category defaults;
- exact budgeting period model;
- whether receipt attachments belong in v1;
- final authentication UX;
- final Android package/application identity.
