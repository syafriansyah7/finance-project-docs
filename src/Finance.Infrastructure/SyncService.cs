using System.Text.Json;
using Finance.Domain;
using Microsoft.EntityFrameworkCore;

namespace Finance.Infrastructure;

public sealed class SyncService(FinanceDbContext db)
{
    private static long _version = DateTime.UtcNow.Ticks;

    public async Task<(IReadOnlyList<SyncPushResult> results, long nextCursor)> PushAsync(Guid userId, IReadOnlyList<SyncPushItem> items, CancellationToken ct)
    {
        var results = new List<SyncPushResult>();
        long maxVersion = 0;

        foreach (var item in items.OrderBy(i => i.ClientUpdatedAt))
        {
            var existing = await db.SyncOperations.FirstOrDefaultAsync(s => s.OperationId == item.OperationId && s.UserId == userId, ct);
            if (existing != null)
            {
                results.Add(new SyncPushResult(item.OperationId, "accepted", existing.ServerVersion));
                maxVersion = Math.Max(maxVersion, existing.ServerVersion);
                continue;
            }

            long version = Interlocked.Increment(ref _version);
            var op = SyncOperation.Create(item.OperationId, userId, item.Entity, item.EntityId, item.Operation, version);
            db.SyncOperations.Add(op);

            try
            {
                await ApplyAsync(userId, item, ct);
                await db.SaveChangesAsync(ct);
                results.Add(new SyncPushResult(item.OperationId, "accepted", version));
                maxVersion = Math.Max(maxVersion, version);
            }
            catch (Exception ex)
            {
                db.SyncOperations.Remove(op);
                results.Add(new SyncPushResult(item.OperationId, "rejected", 0, ex.Message));
            }
        }

        var nextCursor = maxVersion > 0 ? maxVersion : await db.SyncOperations.Where(s => s.UserId == userId).MaxAsync(s => (long?)s.ServerVersion, ct) ?? 0;
        return (results, nextCursor);
    }

    private async Task ApplyAsync(Guid userId, SyncPushItem item, CancellationToken ct)
    {
        if (item.Entity == "transaction" && item.Operation == "create")
        {
            var payload = item.Payload;
            string? GetStr(string k) => payload.TryGetValue(k, out var v) ? v is JsonElement je ? je.ValueKind == JsonValueKind.Null ? null : je.ValueKind == JsonValueKind.String ? je.GetString() : je.ToString() : v?.ToString() : null;
            var typeStr = GetStr("type") ?? "Expense";
            var type = Enum.Parse<TransactionType>(typeStr, true);
            var accountId = Guid.Parse(GetStr("accountId")!);
            var trStr = GetStr("transferAccountId");
            Guid? transferId = string.IsNullOrEmpty(trStr) ? null : Guid.Parse(trStr);
            var catStr = GetStr("categoryId");
            Guid? categoryId = string.IsNullOrEmpty(catStr) ? null : Guid.Parse(catStr);
            var amountStr = GetStr("amount") ?? payload["amount"]?.ToString();
            var amount = long.Parse(amountStr!);
            var currency = GetStr("currency") ?? "IDR";
            var desc = GetStr("description");
            var dateStr = GetStr("transactionDate");
            var date = dateStr != null ? DateTime.Parse(dateStr) : DateTime.UtcNow;

            var tx = Transaction.Create(item.EntityId, userId, type, accountId, transferId, categoryId, amount, currency, desc, date);
            db.Transactions.Add(tx);
        }
        else if (item.Entity == "transaction" && item.Operation == "delete")
        {
            var tx = await db.Transactions.FirstOrDefaultAsync(t => t.Id == item.EntityId && t.UserId == userId, ct);
            if (tx != null) tx.SoftDelete();
        }
        else
        {
            throw new InvalidOperationException($"Unsupported entity {item.Entity} operation {item.Operation}");
        }
    }

    public async Task<(long cursor, IReadOnlyList<object> changes)> PullAsync(Guid userId, long cursor, CancellationToken ct)
    {
        var ops = await db.SyncOperations.Where(s => s.UserId == userId && s.ServerVersion > cursor).OrderBy(s => s.ServerVersion).ToListAsync(ct);
        var changes = new List<object>();
        foreach (var op in ops)
        {
            object? data = null;
            if (op.EntityType == "transaction")
            {
                var tx = await db.Transactions.FirstOrDefaultAsync(t => t.Id == op.EntityId, ct);
                if (tx != null) data = tx;
            }
            changes.Add(new { entity = op.EntityType, operation = op.OperationType, id = op.EntityId, data, serverVersion = op.ServerVersion });
        }
        var next = ops.Count > 0 ? ops.Max(o => o.ServerVersion) : cursor;
        return (next, changes);
    }
}

public record SyncPushItem(Guid OperationId, string Entity, Guid EntityId, string Operation, DateTime ClientUpdatedAt, Dictionary<string, object?> Payload);
public record SyncPushResult(Guid OperationId, string Status, long ServerVersion, string? Error = null);
