# Coding Standards

## 1. General

The project uses C#/.NET as the primary development language.

Priorities:

1. correctness;
2. readability;
3. testability;
4. simplicity;
5. performance where it matters.

Avoid abstraction for its own sake.

## 2. Naming

Follow standard .NET naming conventions:

- PascalCase for types, public members, and methods.
- camelCase for local variables and parameters.
- `_camelCase` for private fields when field style is needed.
- Meaningful names over abbreviations.

Examples:

```csharp
public sealed class TransactionService
{
    private readonly ITransactionRepository _repository;

    public Task<Transaction> CreateAsync(
        CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        // ...
    }
}
```

## 3. Project structure

Prefer clear dependency direction:

```text
Domain
  ↑
Application
  ↑
Infrastructure
  ↑
API / Web / Mobile
```

The domain layer must not depend on database, HTTP, or UI frameworks.

## 4. Async

- Use async/await for I/O.
- Accept `CancellationToken` in application and infrastructure methods where appropriate.
- Do not use `.Result` or `.Wait()` on asynchronous code.

## 5. Nullability

Enable nullable reference types.

Do not suppress nullability warnings without a reason.

## 6. Entity IDs

Use UUID/`Guid` identifiers for entities that can be created on the mobile device offline.

Do not use server-assigned sequential integer IDs for sync-sensitive entities.

## 7. Money

Money must never be represented as `double` or `float`.

Preferred v1 storage:

```text
integer minor/whole currency units
```

For IDR, the stored integer represents rupiah.

## 8. Date/time

- Persist server timestamps in UTC.
- Represent user-facing dates according to the configured local timezone.
- Never rely on server local time implicitly.

## 9. API

- Use DTOs for API contracts.
- Do not expose EF Core entities directly from controllers/endpoints.
- Validate at the API boundary.
- Keep error responses structured.

## 10. Database access

- Prefer EF Core for normal application queries.
- Use parameterized SQL for raw SQL.
- Avoid N+1 query patterns.
- Keep migrations in source control.

## 11. Blazor

- Keep components reasonably small.
- Put business logic in services/application layer, not in UI components.
- Avoid calling the database directly from components.
- Keep UI state explicit.

## 12. MAUI mobile

- Keep SQLite access behind repository/service abstractions.
- Keep sync logic separate from presentation.
- Make offline state visible to the user.
- Never block the UI thread with database/network operations.

## 13. Error handling

Expected business failures should be represented explicitly.

Unexpected exceptions should be logged centrally and translated to safe API errors.

Do not swallow exceptions silently.

## 14. Logging

Logs should answer:

- what happened;
- when;
- which operation/request;
- whether it succeeded;
- a trace/correlation ID.

Never log:

- passwords;
- access tokens;
- refresh tokens;
- database passwords;
- full private secrets.

## 15. Tests

At minimum:

- domain/business unit tests;
- API integration tests;
- sync idempotency tests;
- SQLite repository tests;
- transaction calculation tests;
- offline-to-online end-to-end test.

## 16. Git commits

Prefer small, meaningful commits.

Suggested style:

```text
feat: add transaction creation
fix: prevent duplicate sync operations
refactor: extract budget service
test: cover transfer balance calculation
docs: update API specification
```

## 17. Comments

Comment **why**, not what.

Avoid comments that merely repeat the code.

## 18. Complexity rule

Before introducing a dependency or abstraction, ask:

> Does this reduce complexity for a one-person personal project?

If not, prefer the simpler solution.
