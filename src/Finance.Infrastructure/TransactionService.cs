using Finance.Domain;
using Microsoft.EntityFrameworkCore;

namespace Finance.Infrastructure;

public sealed class TransactionService(FinanceDbContext db)
{
    public async Task<Transaction> CreateAsync(Guid userId, TransactionType type, Guid accountId, Guid? transferAccountId, Guid? categoryId, long amount, string currency, string? description, DateTime transactionDate, Guid? id, CancellationToken ct)
    {
        var accountExists = await db.Accounts.AnyAsync(a => a.Id == accountId && a.UserId == userId, ct);
        if (!accountExists) throw new KeyNotFoundException("Account not found");

        if (transferAccountId != null)
        {
            var destExists = await db.Accounts.AnyAsync(a => a.Id == transferAccountId && a.UserId == userId, ct);
            if (!destExists) throw new KeyNotFoundException("Destination account not found");
        }

        if (categoryId != null)
        {
            var catExists = await db.Categories.AnyAsync(c => c.Id == categoryId && c.UserId == userId, ct);
            if (!catExists) throw new KeyNotFoundException("Category not found");
        }

        var tx = Transaction.Create(id ?? Guid.NewGuid(), userId, type, accountId, transferAccountId, categoryId, amount, currency, description, transactionDate);
        db.Transactions.Add(tx);
        await db.SaveChangesAsync(ct);
        return tx;
    }

    public async Task<Transaction> GetAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var tx = await db.Transactions.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId && t.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException("Transaction not found");
        return tx;
    }

    public async Task<IReadOnlyList<Transaction>> ListAsync(Guid userId, Guid? accountId, Guid? categoryId, TransactionType? type, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct)
    {
        var q = db.Transactions.Where(t => t.UserId == userId && t.DeletedAt == null);
        if (accountId != null) q = q.Where(t => t.AccountId == accountId || t.TransferAccountId == accountId);
        if (categoryId != null) q = q.Where(t => t.CategoryId == categoryId);
        if (type != null) q = q.Where(t => t.Type == type);
        if (from != null) q = q.Where(t => t.TransactionDate >= from);
        if (to != null) q = q.Where(t => t.TransactionDate <= to);
        return await q.OrderByDescending(t => t.TransactionDate).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var tx = await GetAsync(userId, id, ct);
        tx.SoftDelete();
        await db.SaveChangesAsync(ct);
    }

    public async Task<long> BalanceAsync(Guid userId, Guid accountId, CancellationToken ct)
    {
        var txs = await db.Transactions.Where(t => t.UserId == userId && t.DeletedAt == null).ToListAsync(ct);
        return BalanceCalculator.Calculate(accountId, txs);
    }
}
