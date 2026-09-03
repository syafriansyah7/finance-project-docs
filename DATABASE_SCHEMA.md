# Database Schema

## 1. Database choice

Server database: **PostgreSQL**.

Local mobile database: **SQLite**, using the same conceptual domain model where practical but optimized for local operation and sync state.

## 2. Design goals

- simple enough for one user;
- durable historical records;
- UUID identifiers for offline-created entities;
- explicit timestamps;
- soft deletion where historical references matter;
- derived balances rather than duplicated mutable balances;
- safe synchronization.

## 3. Core entities

```text
users
  |
  +-- accounts
  |
  +-- categories
  |
  +-- transactions
  |
  +-- budgets
  |
  +-- sync_operations
```

## 4. PostgreSQL schema draft

This is the v1 draft and should be reviewed before migrations are frozen.

### users

```sql
CREATE TABLE users (
    id UUID PRIMARY KEY,
    email TEXT NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### accounts

```sql
CREATE TABLE accounts (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id),
    name TEXT NOT NULL,
    type TEXT NOT NULL,
    currency CHAR(3) NOT NULL DEFAULT 'IDR',
    is_archived BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX ix_accounts_user_id ON accounts(user_id);
```

Recommended initial account types:

- `Cash`
- `Bank`
- `EWallet`

### categories

```sql
CREATE TABLE categories (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id),
    name TEXT NOT NULL,
    kind TEXT NOT NULL,
    is_archived BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(user_id, name, kind)
);

CREATE INDEX ix_categories_user_id ON categories(user_id);
```

Recommended category kinds:

- `Income`
- `Expense`

Transfers do not require a category.

### transactions

```sql
CREATE TABLE transactions (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id),
    type TEXT NOT NULL,
    account_id UUID NOT NULL REFERENCES accounts(id),
    transfer_account_id UUID NULL REFERENCES accounts(id),
    category_id UUID NULL REFERENCES categories(id),
    amount BIGINT NOT NULL CHECK (amount > 0),
    currency CHAR(3) NOT NULL DEFAULT 'IDR',
    description TEXT NULL,
    transaction_date TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ NULL
);

CREATE INDEX ix_transactions_user_date
    ON transactions(user_id, transaction_date DESC);

CREATE INDEX ix_transactions_account
    ON transactions(account_id);

CREATE INDEX ix_transactions_category
    ON transactions(category_id);
```

Business rules:

- `Income` requires `category_id` and uses a positive amount.
- `Expense` requires `category_id` and uses a positive amount.
- `Transfer` requires `transfer_account_id` and does not use a category.
- `transfer_account_id` must differ from `account_id`.
- A deleted transaction remains queryable for audit/sync purposes until retention rules say otherwise.

### budgets

```sql
CREATE TABLE budgets (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id),
    category_id UUID NOT NULL REFERENCES categories(id),
    month DATE NOT NULL,
    amount BIGINT NOT NULL CHECK (amount >= 0),
    currency CHAR(3) NOT NULL DEFAULT 'IDR',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(user_id, category_id, month)
);

CREATE INDEX ix_budgets_user_month
    ON budgets(user_id, month);
```

The `month` value represents the first day of the target month, e.g. `2026-09-01`.

### sync_operations

```sql
CREATE TABLE sync_operations (
    operation_id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id),
    entity_type TEXT NOT NULL,
    entity_id UUID NOT NULL,
    operation_type TEXT NOT NULL,
    processed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    server_version BIGINT NOT NULL
);

CREATE INDEX ix_sync_operations_user_version
    ON sync_operations(user_id, server_version);
```

This table makes client retries idempotent.

## 5. Server version / sync cursor

A server-side monotonic change version is recommended for synchronization.

Possible implementation:

```text
server_version
  1001
  1002
  1003
  ...
```

A transaction or other sync-relevant mutation receives a new version.

The mobile device records the highest successfully pulled version.

## 6. Balance calculation

For a basic cash-flow model:

```text
Account balance =
  sum(income into account)
  - sum(expense from account)
  + incoming transfers
  - outgoing transfers
```

Do not copy this calculation into a mutable `balance` column unless profiling demonstrates a need later.

## 7. SQLite local schema

SQLite should contain equivalent domain tables plus local synchronization metadata.

Example:

```text
accounts
categories
transactions
budgets
sync_queue
app_metadata
```

### sync_queue

```sql
CREATE TABLE sync_queue (
    operation_id TEXT PRIMARY KEY,
    entity_type TEXT NOT NULL,
    entity_id TEXT NOT NULL,
    operation_type TEXT NOT NULL,
    payload_json TEXT NOT NULL,
    created_at TEXT NOT NULL,
    attempt_count INTEGER NOT NULL DEFAULT 0,
    last_attempt_at TEXT NULL,
    last_error TEXT NULL,
    status TEXT NOT NULL
);
```

Suggested statuses:

- `Pending`
- `Sending`
- `Failed`
- `Synced`

## 8. Schema decisions intentionally postponed

- receipt/attachment table;
- recurring transaction model;
- tags;
- multi-currency exchange rates;
- advanced accounting/ledger tables;
- audit-log granularity.

Do not implement these until requirements justify them.
