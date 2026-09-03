# Security Baseline

## 1. Objective

Protect a personal financial database without introducing unnecessary enterprise complexity.

The system is private and single-user, but financial data is sensitive enough that basic security is mandatory.

## 2. Minimum security baseline

### Transport

- HTTPS only for application traffic.
- Redirect HTTP to HTTPS where appropriate.
- Do not transmit passwords or tokens over plaintext HTTP.

### SSH

- Use SSH keys.
- Disable password authentication after confirming key access.
- Do not expose administrative services unnecessarily.

### Firewall

Expose only the ports required for:

- SSH administration;
- HTTPS.

Do not expose PostgreSQL directly.

### Database

- PostgreSQL listens only on the private/local network.
- Use a dedicated application DB user.
- Use a separate database owner/admin account for migrations when practical.
- Secrets are not committed to source control.

### Mobile credentials

Store authentication tokens using platform-protected storage such as .NET MAUI `SecureStorage`, not plain text files or SQLite tables without protection.

### Passwords

Passwords must be stored as strong one-way password hashes using a modern ASP.NET Core-compatible password hashing mechanism. Never store raw passwords.

## 3. Application security

- Validate all input server-side.
- Authorize every user-owned resource by `user_id`.
- Use parameterized SQL/EF Core rather than string-concatenated SQL.
- Avoid returning secrets in API responses.
- Do not log passwords, access tokens, refresh tokens, or sensitive secrets.
- Use structured logging with correlation/trace IDs.
- Return generic authentication failure messages.

## 4. Sync security

Each sync request is authenticated.

Every operation has an idempotency identifier so that retries cannot create duplicate records.

A device must never be able to submit a transaction for another user.

## 5. Container security

- Run containers with the least privilege practical.
- Do not publish PostgreSQL to the public host interface.
- Keep secrets outside the Git repository.
- Prefer pinned image tags or controlled update procedures.
- Periodically update base images and application dependencies.

## 6. Backups

Backups contain financial data and therefore are sensitive.

- Store backup files securely.
- Do not place backups in a public web directory.
- Keep retention finite and documented.
- Test restoration.

## 7. Security that is intentionally out of scope for v1

- SSO.
- OAuth provider federation.
- Hardware security keys.
- Enterprise SIEM.
- Service mesh.
- Zero-trust network products.
- Complex multi-tenant authorization.

## 8. Incident response for a personal project

If compromise is suspected:

1. Disable public application access.
2. Rotate application credentials and tokens.
3. Review recent authentication and application logs.
4. Restore from a known-good backup if data integrity is uncertain.
5. Update dependencies and close the root cause.
6. Re-enable access only after validation.

## 9. Security review gate before production

Before exposing the API publicly, verify:

- HTTPS works;
- SSH key login works;
- password SSH is disabled;
- PostgreSQL is not public;
- authentication works;
- authorization checks ownership;
- secrets are outside Git;
- backups succeed;
- restore procedure is documented.
