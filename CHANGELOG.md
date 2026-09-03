# Changelog

All notable changes to this project will be documented here.

The format follows the spirit of Keep a Changelog, while remaining lightweight for a personal project.

## [Unreleased]

### Architecture

- Established `.NET MAUI + Blazor Hybrid` as the mobile application approach.
- Established `SQLite` as the mobile local database.
- Established `ASP.NET Core / C#` as the backend platform.
- Established `PostgreSQL` as the server source of truth.
- Established `Blazor` as the laptop web dashboard technology.
- Established `Docker + Docker Compose` for deployment.
- Established `Oracle Cloud Always Free` as the target zero-cost VPS platform.
- Established offline-first mobile data entry.
- Established a single-device sync model for v1.
- Established Google Sheets as optional reporting/export only.
- Established backup as mandatory.

### Added

- Solution `Finance.sln` (net8.0, nullable enable) — `src/Finance.Domain/Application/Infrastructure/Api/Web/Mobile`, `tests/Finance.UnitTests/IntegrationTests`, `deploy/docker-compose.yml` (Caddy + Api + Postgres) + `Caddyfile` + `Dockerfile` + `.env.example` + `global.json`
- Domain: `Account`, `Category`, `User`, `Transaction` (Income/Expense/Transfer validasi, soft-delete), `BalanceCalculator` (derived, transfer +/-), `Budget` (spent/remaining/pct), `SyncOperation` (operationId idempotency, serverVersion)
- Infrastructure: `FinanceDbContext` (Users/Accounts/Categories/Transactions/Budgets/SyncOperations, InMemory fallback), `AuthService` (BCrypt + JWT 24h), `AccountService`, `CategoryService`, `TransactionService`, `SyncService` (push/pull, JsonElement payload handling), `DashboardService`, `BudgetService`
- Mobile: `SyncQueue` (`SyncQueueItem` Pending/Sending/Failed/Synced, `InMemorySyncQueue` — SQLite abstraction ready)
- API: `/health`, `/auth/register|login`, `/accounts` CRUD, `/categories` CRUD, `/transactions` CRUD + `/accounts/{id}/balance`, `/sync/push|pull`, `/dashboard/summary`, `/budgets` — JWT Bearer, 401/404/409/422 mapping
- Web: Blazor dashboard `Home.razor` (Operate, calm, 4 stat cards, category breakdown, recent transactions, accounts)
- Deploy: `backup.sh` (pg_dump + gzip + gpg + rclone -> Oracle Object Storage, retention 7d+4w), `restore.sh`, validated `docker-compose.yml` (postgres internal only, no public port — T15)
- Tests: 18 unit (Account/Category/Transaction/Budget) + 14 integration (Sprint1 auth/ownership, Sprint2 T08/T09, Sprint3 T05/T07 sync, Sprint4 T10 dashboard, Sprint5 offline E2E T02-T06) — total 32, 0 failed
- Docs: `DEPLOYMENT.md` (ap-singapore-1, Caddy, DuckDNS/sslip.io, Gitea Actions, Oracle Object Storage), `OPEN_DECISIONS.md` (6 final), `CONVERSATION_MEMORY.md` §10/11 final, `.agent/plans/plan-finance-v1.md`

## Release policy

Use categories such as:

- Added
- Changed
- Fixed
- Removed
- Security
- Architecture

Do not record secrets, credentials, or private user data in the changelog.
