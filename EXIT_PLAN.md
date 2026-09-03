# Exit Plan

## 1. Purpose

The project must remain portable. A zero-cost VPS is useful, but the application must not become permanently dependent on one provider.

## 2. Provider independence

The application must be deployable to any Linux VPS with:

- Docker/Compose;
- sufficient RAM/CPU;
- persistent storage;
- PostgreSQL support.

Oracle Cloud is a deployment target, not a business-logic dependency.

## 3. Data portability

Canonical data must remain in PostgreSQL.

Required export capabilities:

- PostgreSQL logical backup (`pg_dump`);
- CSV export for transactions;
- optional JSON export for future portability.

Google Sheets is never the only copy of financial data.

## 4. Migration away from Oracle Cloud

High-level procedure:

```text
1. Provision new Linux VPS
2. Install Docker + Compose
3. Restore PostgreSQL backup
4. Deploy application containers
5. Configure DNS/HTTPS
6. Update mobile API endpoint
7. Test sync and dashboard
8. Keep old VPS available until validation succeeds
9. Retire old VPS
```

## 5. Migration away from Docker

Containerization is a deployment convenience, not a data requirement.

The application can ultimately run directly from compiled .NET binaries and PostgreSQL if required.

## 6. Migration away from PostgreSQL

No migration to another database is planned. PostgreSQL is selected because it is mature, portable, and appropriate for the application.

If migration becomes necessary, create exports from domain-level data rather than depending on internal database implementation details.

## 7. Shutting down the project

Before deleting the VPS:

1. Export PostgreSQL data.
2. Export transactions to CSV.
3. Save a final encrypted backup.
4. Verify the backup by restoring it in a temporary environment.
5. Preserve the source repository.
6. Revoke production credentials.
7. Remove the VPS and any external integrations.

## 8. No lock-in rule

Do not design essential application logic around:

- Oracle-specific compute APIs;
- Google Sheets as a required database;
- proprietary hosted databases;
- one vendor's authentication implementation;
- irreversible storage formats.

## 9. Personal data consideration

Financial data belongs to the user. The exit procedure must make it possible to recover the complete transaction history without the original VPS provider.
