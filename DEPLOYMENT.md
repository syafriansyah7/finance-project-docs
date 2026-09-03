# Deployment Guide

## 1. Target platform

Primary target — **Opsi A (tanpa CC, direkomendasikan untuk user tanpa kartu kredit):**

```text
Koyeb (API, Docker, auto-HTTPS) + Neon (PostgreSQL Serverless) — daftar email saja, tanpa CC
Worker opsional di Koyeb, backup ke Google Drive via rclone (15GB free)
```

Alternatif — **Opsi B (butuh CC, jika ada kartu fisik Mastercard/Visa):**

```text
Oracle Cloud Always Free
Ubuntu Linux
Docker + Docker Compose
Region priority for Indonesia: ap-singapore-1 (closest), fallback ap-tokyo-1 / ap-mumbai-1 if Ampere A1 capacity is full
```

The free-tier resource availability and limits must be re-verified before provisioning because cloud-provider terms can change. Always verify Ampere A1 (ARM) capacity in the chosen region before provisioning — Singapore is closest for Indonesia but frequently at capacity. **Jika tidak punya CC, pakai Opsi A (Koyeb+Neon) — tanpa CC, tanpa home lab.**

## 2. Production services

Minimum stack — **Opsi A (Koyeb+Neon, tanpa CC):**

```text
Koyeb Service (finance-api, dari Dockerfile, auto-HTTPS)
Neon PostgreSQL (serverless, 0.5GB free)
Koyeb Cron Job (backup pg_dump → Google Drive, opsional)
```

Minimum Compose stack — **Opsi B (Oracle VPS):**

```text
reverse-proxy (Caddy)
finance-api
postgres
worker (only if background processing requires a separate process)
```

Do not add infrastructure services without a concrete requirement.

## 3. Container boundaries

### Reverse proxy — Caddy (default)

Selected: **Caddy**. Rationale: automatic HTTPS via Let's Encrypt with a single-line config, auto-renewal, and minimal operational overhead for a single-person project.

Responsibilities:

- terminate HTTPS (automatic via Let's Encrypt);
- forward requests to API/web application;
- redirect HTTP → HTTPS;
- optionally serve health endpoints appropriately.

Alternatives considered: Nginx (manual certbot), Traefik (overkill for one Compose stack). Caddy is preferred for v1; switching remains simple due to Compose abstraction.

### Finance API

Responsibilities:

- authentication;
- application/business logic;
- API endpoints;
- sync processing;
- dashboard data queries.

### PostgreSQL

Responsibilities:

- canonical persistence.

PostgreSQL is internal-only and must use a private Docker network / host binding strategy.

### Worker

Only required when background work is implemented, such as scheduled exports, cleanup, or backup orchestration.

## 4. Environment variables

Secrets must be provided at deployment time and not committed to Git.

Examples:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__Default=...
Authentication__...
```

Exact variables depend on implementation.

## 5. Deployment flow

### Opsi A — Koyeb+Neon (tanpa CC, Rp0, tanpa home lab) — DEFAULT untuk user tanpa kartu kredit

```text
1. Build and test locally (dotnet test)
2. Daftar Neon (email saja) → buat project → copy ConnectionStrings__Default (pooled)
3. Daftar Koyeb (email saja) → Create Service → Build from GitHub/Dockerfile (src/Finance.Api/Dockerfile)
4. Set env di Koyeb: ConnectionStrings__Default (Neon), Jwt__SigningKey (32+ chars), ASPNETCORE_ENVIRONMENT=Production
5. Koyeb auto-build + deploy + auto-HTTPS (https://xxx.koyeb.app)
6. Run migrations (Koyeb Job: dotnet ef database update atau auto-migrate on startup)
7. Verify https://xxx.koyeb.app/health
8. Verify authenticated API + dashboard
9. Verify mobile sync (ganti API base URL di MAUI ke Koyeb URL)
10. Setup backup: Koyeb Cron → pg_dump Neon → rclone → Google Drive
```

### Opsi B — Oracle VPS (butuh CC, Caddy)

v1 manual (default — Rp0 jika punya CC):

```text
1. Build and test locally
2. Build production containers
3. Push images or build on VPS
4. Pull/prepare environment configuration
5. Apply database migrations
6. Start Compose stack (Caddy + API + PostgreSQL)
7. Verify health endpoint
8. Verify HTTPS (Caddy auto-TLS)
9. Verify authenticated API
10. Verify dashboard
11. Verify mobile sync
12. Confirm backup job
```

Optional FOSS automation (v2, still Rp0): **Gitea Actions** (self-hosted Gitea on the same VPS or local) — workflow builds the image and runs `ssh → docker compose pull && up -d`. No GitHub Actions or paid CI required.

## 6. Database migration rule

Database migrations must be stored in source control.

Never manually edit production tables as the normal deployment process.

Before destructive migrations:

1. create backup;
2. verify backup;
3. apply migration;
4. verify application health.

## 7. Rollback

For application rollback:

- deploy the previous known-good image/tag;
- do not automatically roll back a database migration unless explicitly designed and tested.

For database recovery:

- restore from backup to a controlled environment first;
- validate;
- then perform production recovery.

## 8. Backup deployment

At minimum, automate PostgreSQL logical backups.

Selected destination — **Opsi A (tanpa CC): Google Drive via rclone (15GB free)** — untuk Koyeb+Neon. **Opsi B (butuh CC): Oracle Object Storage Always Free (20 GB)** — untuk Oracle VPS.

Recommended pattern — Opsi A:

```text
Koyeb Cron (daily)
  -> pg_dump $ConnectionStrings__Default (Neon)
  -> gzip + gpg encrypt
  -> rclone → Google Drive (private folder)
  -> retention cleanup
```

Recommended pattern — Opsi B:

```text
cron/timer
  -> pg_dump
  -> gzip + encrypt
  -> rclone / oci cli → Oracle Object Storage (private bucket)
  -> retention cleanup
```

Retention (final): **daily 7 days + weekly 4 weeks** — auto-rotate; adjust if bucket approaches capacity.

Verify restore periodically by restoring to a temporary environment (Neon branch atau local postgres).

## 8a. DNS / Domain — Rp0 murni

No paid domain required for v1.

Options:
- **DuckDNS** (`*.duckdns.org`) — free subdomain, 2-minute setup.
- **sslip.io** (`<IP>.sslip.io` e.g. `152.70.x.x.sslip.io`) — zero-registration, derives domain from IP.

Both work with Caddy + Let's Encrypt for HTTPS. Upgrading later to a paid domain + Cloudflare DNS (Free) is an optional path, not required for Rp0 operation. Cloudflare Tunnel is not used for v1 (adds dependency); Caddy on the VPS handles TLS directly.

## 9. Monitoring

Minimum checks:

- API `/health`;
- disk usage;
- PostgreSQL availability;
- container status;
- backup success/failure.

Avoid deploying heavyweight monitoring stacks unless needed.

## 10. Update process

Keep production changes controlled:

```text
backup
-> deploy
-> migrate
-> health-check
-> sync-test
-> monitor
```

## 11. Disaster recovery

The recovery target is:

```text
new Linux VPS
+ Docker
+ Compose
+ PostgreSQL restore
+ application deployment
```

No Oracle-specific data format should be required.
