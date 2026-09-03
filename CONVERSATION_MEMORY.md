# Conversation Memory / AI Agent Handoff

## 0. Purpose

This document is a complete explicit handoff of the project decisions, constraints, reasoning, and open questions established in the project discussion so another AI coding agent can continue without repeatedly asking the same architectural questions.

This is a **project context document**, not a record of hidden reasoning. It contains the explicit decisions and requirements communicated in the conversation.

---

## 1. Project intent

The user wants to build a **private personal finance application** for personal use.

The user wants the project to operate with **zero monetary investment / Rp0** as far as technically and operationally possible.

The application is not intended to be a commercial SaaS product, enterprise finance platform, or multi-user household accounting system.

The project should favor simplicity, maintainability, portability, and practical daily use over enterprise architecture.

---

## 2. Device roles

### Phone

The **phone is the primary operational device**.

It is where the user will primarily:

- enter income;
- enter expenses;
- enter transfers;
- view recent transactions;
- use the application while offline.

There is currently **one primary phone only**.

### Laptop

The laptop is **not the main transaction-entry device**.

The laptop is primarily for:

- monitoring the data entered through the phone;
- viewing the dashboard;
- reviewing reports;
- inspecting transactions;
- understanding financial trends.

The laptop should therefore have a strong monitoring/dashboard experience rather than a mobile-style quick-entry UX.

---

## 3. Offline requirement

A major requirement is:

> The phone must still be able to record transactions when there is no internet connection.

The transaction must be saved locally and shown to the user immediately.

When connectivity returns, the app synchronizes the pending data to the VPS automatically or through a retry action.

The user should not need internet access merely to record an everyday transaction.

---

## 4. Mobile technology decision — FINAL BASELINE

The user explicitly chose **Option B**:

> **.NET MAUI + Blazor Hybrid**

This supersedes the earlier idea of making the phone primarily a Blazor PWA.

The mobile application baseline is:

```text
Android
  -> .NET MAUI
  -> Blazor Hybrid
  -> SQLite
  -> Offline sync queue
```

The goal is to keep C#/.NET central to the project while having a real native mobile application shell.

Do not casually replace MAUI Blazor Hybrid with React Native, Flutter, or a pure PWA. Such a change requires an explicit architecture review.

---

## 5. Local database decision — FINAL BASELINE

The user selected **SQLite**.

SQLite is the mobile local database and must allow the application to continue operating offline.

Suggested local entities include:

```text
accounts
categories
transactions
budgets
sync_queue
```

The local database must survive application restarts and retain pending transactions until they are synchronized successfully.

---

## 6. Server/backend decisions — FINAL BASELINE

Backend:

> **ASP.NET Core / C#**

Server database:

> **PostgreSQL**

PostgreSQL is the **server source of truth / canonical data store**.

The system should avoid manually synchronized mutable balances whenever possible. Balances should be derived from transaction/account data.

---

## 7. Web/dashboard decision — FINAL BASELINE

The laptop dashboard will be built as a **Blazor web application** using the same .NET/C# ecosystem.

Conceptually:

```text
ASP.NET Core
  + Blazor Web
  -> laptop dashboard
```

The dashboard should provide:

- current account balances;
- income;
- expenses;
- net change;
- expense breakdown by category;
- recent transactions;
- budget progress;
- reporting.

The user asked whether Google Sheets should be the dashboard. The explicit decision was:

> **Build our own dashboard. Do not make Google Sheets the primary dashboard.**

Google Sheets is optional reporting/export only.

---

## 8. Google Sheets decision

Google Sheets is **not** the database and not the critical application path.

Possible role:

```text
PostgreSQL
   -> optional export/reporting
   -> Google Sheets
```

The application must continue working if Google Sheets is unavailable or removed.

Google Sheets must never be the sole backup or canonical copy of financial data.

---

## 9. Docker decision — FINAL BASELINE

The user asked whether to use Docker or Podman.

Decision:

> **Docker + Docker Compose**

Podman is not the default.

Kubernetes is explicitly unnecessary for this project.

Target VPS stack:

```text
Docker Compose
  -> reverse proxy
  -> ASP.NET Core application/API
  -> PostgreSQL
  -> optional worker/background service
```

Prefer one simple Compose stack over distributed infrastructure.

---

## 10. VPS decision — FINAL BASELINE (murni Rp0)

The user wants a **free but powerful VPS** and does not want to spend money on the project — **murni Rp0 permanen**, familiar with Gitea, located in Indonesia.

Target:

> **Oracle Cloud Always Free — region priority ap-singapore-1 (closest), fallback ap-tokyo-1 / ap-mumbai-1**

Earlier discussion identified Oracle Cloud Always Free Ampere A1 resources as attractive for this project, with a target of approximately 2 OCPU and up to 12 GB RAM under the stated free-tier constraints.

- Reverse proxy: **Caddy** (auto-HTTPS via Let's Encrypt).
- DNS/domain: **Rp0 murni — DuckDNS / sslip.io** (paid domain + Cloudflare DNS is optional upgrade, not required for v1).
- Deploy: **manual v1** (`docker compose pull && up -d`); optional FOSS automation v2 via **Gitea Actions** (self-hosted, still Rp0). Cloudflare Tunnel/Pages/Workers not used for v1.
- Backup: **Oracle Object Storage Always Free** (see §11).

Important: free-tier resource availability, limits, region capacity, and provider terms can change. Verify Ampere A1 capacity in the chosen region before provisioning.

The application itself must remain provider-portable.

---

## 11. Backup decision — FINAL

The user asked whether VPS backup is necessary.

Decision:

> **Yes. Backup is required — destination: Oracle Object Storage Always Free (20 GB), murni Rp0.**

Region priority for Indonesia: `ap-singapore-1` (closest), fallback `ap-tokyo-1` / `ap-mumbai-1` if Ampere A1 capacity is full.

Retention: **daily 7 days + weekly 4 weeks** (auto-rotate).

Pattern:

```text
PostgreSQL
  -> pg_dump
  -> gzip + encrypt
  -> rclone / oci cli → Oracle Object Storage (private bucket)
  -> retention cleanup
```

Verify restore periodically by restoring to a temporary environment.

---

## 12. Security decision

The user explicitly does **not** want overly complicated security.

However, security cannot be ignored.

The agreed direction is:

- HTTPS in production;
- SSH keys for administration;
- basic firewall;
- PostgreSQL private/not publicly exposed;
- strong secrets/passwords;
- application authentication;
- secure local credential/token storage;
- backups.

Do NOT introduce enterprise-level mechanisms such as service mesh, Kubernetes security layers, complex zero-trust infrastructure, or heavyweight WAF architecture unless an actual requirement appears.

"Simple" means minimal and correct, not insecure.

---

## 13. Financial domain simplicity

The user said they do not understand finance/accounting deeply.

Therefore the application must not assume formal accounting expertise.

The intended mental model is simple:

### Account

Where money is held.

Examples:

- Cash
- Bank
- E-Wallet

### Transaction types

- Income
- Expense
- Transfer

### Category

What the money is for.

Examples:

- Food
- Transport
- Bills
- Shopping
- Entertainment
- Health
- Salary / Income
- Other

### Budget

A simple category spending limit for a defined period.

The app should hide unnecessary accounting complexity.

---

## 14. Transaction semantics

Transfers are not income and not expenses.

Example:

```text
Cash -> Bank
```

This moves money between accounts and should not artificially increase income or expense totals.

Balances should be derived from valid transactions.

Avoid storing a manually edited balance as the primary financial state.

---

## 15. Single-device sync simplification

Only one primary mobile device exists in v1.

Therefore the project does NOT need:

- CRDT;
- multi-master merge;
- peer-to-peer sync;
- cross-device conflict resolution;
- multiple mobile device reconciliation.

Sync still needs:

- durable local queue;
- retries;
- idempotency;
- server validation;
- a sync cursor/version if useful;
- clear status UI.

Preferred model:

```text
HP SQLite
  -> pending queue
  -> API
  -> PostgreSQL
  -> ACK
  -> synced
```

---

## 16. UUID/idempotency requirement

Locally created, sync-sensitive entities should use GUID/UUID IDs.

Server-assigned sequential IDs should not be required for offline-created transactions.

Every retryable sync operation should carry a unique operation ID.

Submitting the same operation twice must not create duplicate financial records.

---

## 17. Primary user workflow

The mobile transaction workflow should be very fast.

Preferred basic interaction:

```text
Amount
Category
Account
Save
```

Date/time should default automatically.

Description/note should be optional.

The user should not be forced to fill many fields to record a simple expense.

---

## 18. Reference repositories

Three previously discussed open-source finance projects remain **reference material**, not application dependencies.

### Actual Budget

Use as conceptual reference for:

- local-first architecture;
- offline behavior;
- synchronization concepts;
- budgeting UX.

Do not automatically adopt Actual Budget as the backend or frontend.

### Firefly III

Use as domain/accounting reference for concepts such as:

- accounts;
- transactions;
- categories;
- budgets;
- transfers;
- recurring transactions;
- tags/rules if needed later.

Do not copy the entire accounting complexity into v1.

### ezBookkeeping

Use as reference for:

- simplicity;
- personal-finance workflows;
- responsive UI/PWA ideas;
- self-hosted deployment;
- lightweight architecture.

Again, it is a reference, not a runtime dependency.

---

## 19. Non-goals / explicitly rejected complexity

Do not add these without explicit new requirements:

- Kubernetes;
- microservices split by domain;
- Redis merely because it is common;
- Kafka/event streaming infrastructure;
- Elasticsearch;
- enterprise identity management;
- multi-device merge algorithms;
- bank API integrations in v1;
- Google Sheets as database;
- enterprise accounting UI;
- paid cloud dependencies;
- unnecessary observability stacks.

---

## 20. Current architecture baseline

The agreed architecture is:

```text
                         INTERNET
                             |
                          HTTPS
                             |
                 Oracle Cloud Always Free
                             |
                    Docker + Compose
                             |
                  ASP.NET Core / C#
                             |
                        PostgreSQL
                             |
              +--------------+--------------+
              |                             |
              v                             v
       Android Mobile                  Blazor Web
       .NET MAUI                       Dashboard
       Blazor Hybrid                        |
       SQLite                               v
       Offline Queue                     Laptop
              |
              |
           Sync API
              |
              v
          PostgreSQL
```

Google Sheets is an optional side path:

```text
PostgreSQL -> optional export/report -> Google Sheets
```

---

## 21. Documentation baseline already created

Core documents:

- `README.md`
- `ARCHITECTURE.md`
- `API_SPEC.md`
- `DATABASE_SCHEMA.md`
- `SECURITY.md`
- `SETUP.md`
- `CODING_STANDARDS.md`
- `AGENTS.md`
- `CHANGELOG.md`
- `LICENSE.md`
- `EXIT_PLAN.md`
- `GLOSSARY.md`

Additional design documents:

- `REQUIREMENTS.md`
- `SYNC_SPEC.md`
- `UI_SPEC.md`
- `TEST_PLAN.md`
- `DEPLOYMENT.md`
- `CONVERSATION_MEMORY.md`

---

## 22. Remaining open decisions

These are NOT approved and should not be silently finalized by an AI agent:

1. Final product/application name.
2. Final license choice and copyright holder text.
3. Exact .NET SDK / MAUI version to pin when implementation starts.
4. Final authentication implementation and token lifetime.
5. ~~Exact PostgreSQL backup destination~~ — **FINAL: Oracle Object Storage Always Free (Rp0)**.
6. ~~Exact backup retention policy~~ — **FINAL: daily 7 days + weekly 4 weeks**.
7. Android-only v1 versus future iOS support strategy.
8. Exact starter categories.
9. Exact budget period behavior.
10. Receipt attachments in v1 or later.
11. Final UI visual identity/theme.
12. Exact API route set beyond the initial contract.
13. Exact SQLite library/provider and repository implementation.
14. Exact sync cursor implementation.

Decided in this session (murni Rp0, Gitea-familiar, Indonesia): Caddy as reverse proxy, DuckDNS/sslip.io for DNS, Gitea Actions as optional FOSS CI (manual v1), ap-singapore-1 region priority — see §10/§11 and DEPLOYMENT.md. Vercel/Cloudflare not used as VPS replacement.

An AI agent should clearly label these as decisions to discuss instead of quietly choosing them.

---

## 23. Agent operating rules

When continuing this project:

1. Preserve the agreed architecture unless the user explicitly reopens it.
2. Prefer simple solutions suitable for one person.
3. Keep C#/.NET central to backend and UI.
4. Treat the phone as offline-first.
5. Treat PostgreSQL as canonical server storage.
6. Treat SQLite as the local mobile database.
7. Keep Google Sheets optional.
8. Never make PostgreSQL publicly accessible.
9. Make sync retry-safe and idempotent.
10. Never introduce multi-device conflict logic unless the requirements change.
11. Do not spend money or introduce paid service dependencies without explicit approval.
12. Do not store secrets in source control.
13. Do not make architecture decisions simply because a common enterprise pattern exists.
14. When a decision remains open, state it explicitly and choose the simplest reversible implementation for local development only if progress requires it.

---

## 24. Definition of architectural success

The project succeeds architecturally when all of these are true:

```text
Phone can record expenses without internet.
        |
        v
SQLite safely stores them.
        |
        v
Internet returns.
        |
        v
Pending records sync exactly once.
        |
        v
PostgreSQL becomes the canonical server state.
        |
        v
Laptop dashboard shows correct data.
        |
        v
Database can be backed up and restored.
        |
        v
System can be moved to another VPS.
```

The system should remain understandable to one developer/user.
