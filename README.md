# Personal Finance App

A private, single-user personal finance application designed around **mobile-first, offline-first data entry** and a **web dashboard for monitoring**.

## Project intent

The phone is the primary operational device. The laptop is primarily a monitoring/dashboard device. The project targets **Rp0 operational cost** wherever technically and operationally possible.

## Architecture at a glance

```text
Android Phone
  .NET MAUI + Blazor Hybrid
  SQLite
  Offline Queue
       |
       | HTTPS Sync
       v
Oracle Cloud Always Free (target)
  Docker Compose
  ASP.NET Core / C#
  PostgreSQL
       |
       v
Blazor Web Dashboard
       |
     Laptop
```

Optional reporting path:

```text
PostgreSQL -> optional export/reporting -> Google Sheets
```

Google Sheets is not part of the core data path.

## Core principles

1. **Phone first:** daily transaction entry happens on the phone.
2. **Offline first:** the phone can record transactions without internet access.
3. **SQLite locally:** local mobile state is stored in SQLite.
4. **PostgreSQL centrally:** PostgreSQL is the server source of truth.
5. **Single device in v1:** no multi-device conflict-resolution system.
6. **Transfers are not income/expense:** financial reports must preserve this semantic.
7. **Custom dashboard:** the laptop reads the application's own Blazor dashboard.
8. **Google Sheets is optional:** never required for operation or recovery.
9. **Simple security:** secure the system correctly without enterprise overengineering.
10. **Backups are mandatory:** personal financial history must be recoverable.
11. **Docker Compose:** simple, reproducible deployment without Kubernetes.
12. **Portability:** avoid provider lock-in.

## Product model

The user should not need accounting expertise. The primary concepts are:

```text
Account
Category
Transaction
Budget
```

Transaction types:

```text
Income
Expense
Transfer
```

## Documentation

### Core

- [ARCHITECTURE.md](./ARCHITECTURE.md) — system architecture and boundaries
- [API_SPEC.md](./API_SPEC.md) — HTTP API contract
- [DATABASE_SCHEMA.md](./DATABASE_SCHEMA.md) — database design
- [SECURITY.md](./SECURITY.md) — security baseline
- [SETUP.md](./SETUP.md) — development setup
- [DEPLOYMENT.md](./DEPLOYMENT.md) — VPS deployment
- [CODING_STANDARDS.md](./CODING_STANDARDS.md) — coding conventions
- [AGENTS.md](./AGENTS.md) — AI coding-agent instructions

### Product/design

- [REQUIREMENTS.md](./REQUIREMENTS.md) — functional/non-functional requirements
- [SYNC_SPEC.md](./SYNC_SPEC.md) — offline/synchronization behavior
- [UI_SPEC.md](./UI_SPEC.md) — mobile and laptop UI behavior
- [TEST_PLAN.md](./TEST_PLAN.md) — verification strategy

### Project governance

- [CHANGELOG.md](./CHANGELOG.md) — project changes
- [LICENSE.md](./LICENSE.md) — license draft/decision
- [EXIT_PLAN.md](./EXIT_PLAN.md) — portability and shutdown plan
- [GLOSSARY.md](./GLOSSARY.md) — terminology
- [CONVERSATION_MEMORY.md](./CONVERSATION_MEMORY.md) — explicit AI-agent handoff/context

## Reference projects

The project may use these projects as references:

- **Actual Budget** — local-first and synchronization concepts.
- **Firefly III** — financial domain/accounting concepts.
- **ezBookkeeping** — simplicity and self-hosted personal-finance workflow.

They are references, not required runtime dependencies.

## Current status

**Architecture baseline approved. Detailed product/data/sync/UI design documented. Implementation has not started.**

## Explicit non-goals for v1

- multi-user finance;
- multiple primary phones;
- bank API integrations;
- enterprise accounting complexity;
- Kubernetes;
- paid infrastructure dependencies;
- Google Sheets as the primary data store;
- cross-device conflict resolution.

## Open decisions

See `CONVERSATION_MEMORY.md` for the explicit list of unresolved decisions. Do not silently convert those into permanent architecture decisions.
