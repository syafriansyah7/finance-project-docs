# AGENTS.md

## Purpose

This file gives instructions to AI coding agents working on the project.

## Project context

This is a private, single-user personal finance application.

Primary device:

- one Android phone;
- .NET MAUI + Blazor Hybrid;
- SQLite;
- offline-first data entry.

Server:

- ASP.NET Core / C#;
- PostgreSQL;
- Docker Compose;
- Oracle Cloud Always Free target.

Monitoring:

- Blazor web dashboard on laptop.

Google Sheets is optional and is never the primary database.

## Non-negotiable architectural rules

1. Do not replace PostgreSQL with Google Sheets as the core database.
2. Do not remove offline-first behavior from the mobile app without an explicit architectural decision.
3. Do not introduce multi-device conflict resolution unless requirements change.
4. Do not introduce microservices for v1.
5. Do not expose PostgreSQL directly to the public internet.
6. Do not store raw passwords.
7. Do not store money as floating-point values.
8. Do not create duplicate server records when a mobile sync operation is retried.
9. Keep mobile-generated UUIDs stable across retries.
10. Keep business logic out of Blazor UI components when it belongs in the application/domain layers.

## Before changing architecture

An agent must first check:

- `ARCHITECTURE.md`
- `API_SPEC.md`
- `DATABASE_SCHEMA.md`
- `SECURITY.md`

If an implementation request conflicts with those documents, describe the conflict before making the architectural change.

## Data rules

Transactions are immutable historical facts as much as possible. Prefer correcting/voiding with explicit behavior over silent destructive rewrites.

Transfers move value between accounts and should not inflate income or expense totals.

Balances are derived from transactions.

## Offline/sync rules

Every offline-created sync-sensitive entity must have a stable UUID.

Every queued operation must have a stable idempotency/operation ID.

A retry must be safe.

The app must remain usable when the network is unavailable.

## Security rules

Never commit:

- credentials;
- private keys;
- JWT signing secrets;
- connection passwords;
- production `.env` files.

Use placeholders/examples in documentation.

## Coding rules

Follow `CODING_STANDARDS.md`.

Prefer existing project patterns over introducing a new framework or library.

Do not rewrite working modules merely to match personal style.

## Testing expectations

Any change to transaction creation, balances, budgets, sync, authentication, or persistence should include or update tests.

Minimum sync test cases:

- one successful push;
- repeated push of the same operation ID;
- temporary network failure followed by retry;
- app restart with pending queue;
- multiple pending transactions processed in order.

## Documentation expectations

When a behavior changes, update the relevant documentation in the same change where practical.

Keep `CHANGELOG.md` current for user-visible or architectural changes.

## Project context priority

Before making architecture-affecting changes, read `CONVERSATION_MEMORY.md` and treat its approved decisions as project constraints. Read `REQUIREMENTS.md`, `SYNC_SPEC.md`, and `UI_SPEC.md` before implementing corresponding features. Treat entries under "Remaining open decisions" in `CONVERSATION_MEMORY.md` as unresolved unless the user explicitly decides them.

## Active skills (tetap untuk sesi berikutnya)

Skills berikut ditetapkan untuk efisiensi dan minimalisasi code (ponytail + anti-AI), sesuai keputusan sesi ini:

- `ponytail` — minimalisasi kode, hapus komentar AI, sederhanakan struktur
- `antislop` + `antislop-code` + `antislop-ui` — filter UI/copy/code slop, jaga purposeful design
- `tdd` — hybrid TDD: TDD untuk Domain/Application (balance, transfer, sync idempotency), code-dulu untuk Infra/UI
- `systematic-debugging` — root cause dulu sebelum fix (sync queue, offline, network)
- `verification-before-completion` — evidence before claim, gate T01-T15 sebelum rilis
- `domain-modeling` — pertajam Account/Category/Transaction/Budget/Transfer di CONTEXT.md/GLOSSARY
- `codebase-design` — deep modules, seam discipline, Domain -> Application -> Infrastructure
- `impeccable` — UI MAUI Blazor Hybrid + Blazor dashboard (Operate mode, calm/readable, craft floor)

Muatan: load skill via `skill` tool di awal sesi yang menyentuh area tersebut. Jangan load semua sekaligus jika tidak diperlukan.
