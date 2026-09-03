# Plan: Finance App v1 - Rp0 Murni (Gitea, Caddy, Oracle)

## Konteks
Single-user, phone offline-first (MAUI Blazor Hybrid + SQLite), server ASP.NET Core + PostgreSQL di Oracle Always Free ap-singapore-1, dashboard Blazor di laptop. Dokumen baseline sudah final, tinggal eksekusi kode. Target Rp0 permanen, murni tanpa domain berbayar.

## Keputusan final (tidak ditanya lagi)
- VPS: Oracle Always Free, ap-singapore-1 prioritas, fallback Tokyo/Mumbai. Verifikasi kapasitas Ampere A1 sebelum provisioning.
- Reverse proxy: Caddy (auto-HTTPS Let's Encrypt, 1 baris config).
- DNS: DuckDNS / sslip.io (Rp0). Cloudflare DNS/Tunnel tidak dipakai v1.
- Backup: Oracle Object Storage Always Free 20GB via rclone/oci, retention 7 harian + 4 mingguan.
- CI: manual v1 (`docker compose pull && up -d`), Gitea Actions opsional v2 (self-host, FOSS).
- Branch: `main` saja. Commit style `feat/fix/test/docs` sesuai CODING_STANDARDS.md.
- Testing: hybrid. TDD untuk Domain/Application (balance derived, transfer, sync idempotency), code-dulu untuk Infra/API/Web/Mobile.

## Struktur repo (Sprint 0)
```
/src/Finance.Domain        # tanpa dependensi DB/HTTP
/src/Finance.Application   # logic + sync rules, CancellationToken
/src/Finance.Infrastructure # EF Core, Npgsql, SQLite
/src/Finance.Api           # DTO, validation, /api/v1 + /sync/pull + /sync/push
/src/Finance.Web           # Blazor dashboard
/src/Finance.Mobile        # MAUI Blazor Hybrid + sync_queue
/tests/Finance.UnitTests
/tests/Finance.IntegrationTests
/deploy/docker-compose.yml
/deploy/docker-compose.dev.yml
/deploy/Caddyfile
```

## Urutan eksekusi
### Sprint 0 - Scaffolding
- `dotnet new sln` + 6 project, nullable enable, EF Core, Npgsql
- `docker-compose.dev.yml` (postgres only) + `docker-compose.yml` (caddy+api+postgres)
- Migrasi awal: users, accounts, categories, transactions, budgets, sync_operations (DATABASE_SCHEMA.md)
- `GET /health` + `GET /api/v1/accounts` placeholder
- Gate: `dotnet build && dotnet test` hijau, `/health` 200

### Sprint 1 - Auth + Accounts/Categories
- Domain: Account/Category entity, validasi user_id
- API: POST/GET /accounts, /categories, JWT bearer, SecureStorage di mobile
- TDD: validasi, arsip, UNIQUE(user_id,name,kind)
- Gate: integration test auth + ownership

### Sprint 2 - Transactions + Balance
- Domain: Income/Expense butuh category_id, Transfer butuh transfer_account_id != account_id, amount BIGINT >0, currency IDR
- Balance derived: `sum(income) - sum(expense) + incoming transfers - outgoing transfers` (DATABASE_SCHEMA.md:195)
- TDD: balance calc, transfer tidak masuk income/expense, validasi amount
- Gate: T08, T09

### Sprint 3 - Offline + Sync (inti)
- Mobile: SQLite + sync_queue (operation_id UUID, payload_json, status Pending/Sending/Failed/Synced) - SYNC_SPEC.md
- API: POST /sync/push (idempoten via sync_operations.operation_id), GET /sync/pull?cursor
- Retry: bounded exponential backoff 5s/15s/60s/5m, bedakan transient vs permanent
- TDD: sync state, idempotency (kirim operationId sama 2x -> 1 record), retry
- Gate: T02-T07 (offline create, close/reopen, retry, no duplicate, permanent failure)

### Sprint 4 - Dashboard Blazor
- API: GET /dashboard/summary?month, categoryExpenses
- Web: Income/Expense/Net, balance per account, expense by category, recent transactions, budget progress
- Code-dulu, integration test query
- Gate: T10

### Sprint 5 - Deploy Oracle + Backup + Gitea
- VPS: Ubuntu + Docker + Caddyfile (reverse proxy ke api, auto-TLS via DuckDNS/sslip.io)
- ENV: ConnectionStrings__Default, Jwt__SigningKey via env (tidak commit)
- Backup: cron `pg_dump | gzip | rclone -> Oracle Object Storage` + retention + test restore
- Gitea self-host (opsional, 1 container)
- Gate: T11-T15 (HTTPS, Postgres private, backup/restore), SETUP.md:117 offline test end-to-end

## Aturan kerja harian
```
code (TDD untuk Domain) -> dotnet test -> docker compose up -> test offline manual (matikan internet -> 3 transaksi -> restart app -> nyalakan internet -> cek 1 record) -> commit kecil -> push main
```

## Verifikasi tiap sprint
- `grep -r CODING_STANDAR` = 0, `grep -r sync/changes` = 0 (sudah bersih)
- `dotnet test` untuk Domain/Application
- `docker compose -f deploy/docker-compose.dev.yml up -d && dotnet ef database update`
- Release gate TEST_PLAN.md:91 - jangan rilis jika offline hilang, duplikat mungkin, restore belum verified, PostgreSQL public, HTTPS missing

## File yang berubah di sesi ini (sudah dieksekusi)
- DEPLOYMENT.md: region, Caddy, Gitea Actions, DNS Rp0, backup Oracle
- OPEN_DECISIONS.md: 6 item dicentang final
- CONVERSATION_MEMORY.md: §10/§11/§22 final
- .agent/plans/plan-finance-v1.md (file ini)

## Tidak dikerjakan
- Kode aplikasi (Sprint 0-5) - eksekusi berikutnya setelah plan ini disetujui
- Design system / warna / typography - butuh DESIGN.md jika mau, saat ini ikuti UI_SPEC calm/readable
