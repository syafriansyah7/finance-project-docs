# Setup Guide

## 1. Prerequisites

### Development machine

Install:

- .NET SDK compatible with the selected project baseline;
- Visual Studio or another supported .NET IDE/editor;
- .NET MAUI workloads;
- Android SDK/emulator or a physical Android phone for testing;
- Docker Desktop;
- Git.

Exact versions should be pinned when implementation begins.

### VPS

Target:

- Oracle Cloud Always Free;
- Ubuntu Linux;
- Docker Engine;
- Docker Compose plugin.

## 2. Repository structure

Proposed structure:

```text
/src
  /Finance.Api
  /Finance.Web
  /Finance.Mobile
  /Finance.Application
  /Finance.Domain
  /Finance.Infrastructure
/tests
  /Finance.UnitTests
  /Finance.IntegrationTests
/deploy
  docker-compose.yml
  /proxy
/docs
  *.md
```

This is a logical starting point, not a mandatory final folder layout.

## 3. Local development

### Clone

```bash
git clone <repository-url>
cd <repository-directory>
```

### Start PostgreSQL

Use Docker Compose for local infrastructure:

```bash
docker compose -f deploy/docker-compose.dev.yml up -d
```

### Configure secrets

Use local user secrets/environment variables. Do not commit credentials.

Example configuration categories:

```text
ConnectionStrings__Default
Jwt__SigningKey
Database__Password
```

### Apply migrations

```bash
dotnet ef database update
```

The exact startup project and migration project should be defined once the solution is created.

## 4. Run backend

```bash
dotnet run --project src/Finance.Api
```

Verify:

```text
GET /health
```

## 5. Run web dashboard

```bash
dotnet run --project src/Finance.Web
```

The web app should call the local API during development.

## 6. Run mobile app

Open the MAUI project in the IDE, select an Android device/emulator, and run.

The mobile application should work against the development API over the configured development endpoint.

For physical Android devices, use a reachable development host or a suitable local tunnel. Never hard-code a laptop-localhost API URL into production builds.

## 7. Local offline test

Required test flow:

1. Sign in.
2. Create an expense while online.
3. Confirm it appears on the API/dashboard.
4. Disable phone internet.
5. Create several transactions.
6. Close/reopen the app.
7. Confirm transactions remain in SQLite.
8. Re-enable internet.
9. Confirm transactions sync exactly once.
10. Confirm no duplicates appear in PostgreSQL.

## 8. VPS deployment

### Server bootstrap

1. Create Oracle Cloud compute instance.
2. Create SSH key access.
3. Update OS packages.
4. Configure firewall/security list.
5. Install Docker Engine and Compose.
6. Create an application deployment directory.
7. Copy deployment configuration without secrets.
8. Set production secrets through environment/secret mechanism.
9. Start containers.
10. Configure HTTPS.
11. Run database migrations.
12. Verify `/health` and login.

## 9. Production deployment

```bash
docker compose -f deploy/docker-compose.yml pull
docker compose -f deploy/docker-compose.yml up -d
```

Run migrations through the agreed release process before serving new application versions.

## 10. Backup setup

Configure a scheduled PostgreSQL dump, for example daily.

Conceptual command:

```bash
pg_dump ... > finance-YYYY-MM-DD.sql
```

The final implementation should:

- create backups automatically;
- rotate old backups;
- protect backup files;
- verify backup success;
- document restoration.

## 11. Deployment principles

- Infrastructure is reproducible from Compose configuration.
- Production secrets do not live in Git.
- Database migrations are versioned.
- Every production release should be reversible where practical.
- Do not make destructive schema changes without a backup.
