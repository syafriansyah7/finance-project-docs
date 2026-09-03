using Finance.Domain;
using Microsoft.EntityFrameworkCore;

namespace Finance.Infrastructure;

public sealed class AccountService(FinanceDbContext db)
{
    public async Task<IReadOnlyList<Account>> ListAsync(Guid userId, CancellationToken ct)
        => await db.Accounts.Where(a => a.UserId == userId && !a.IsArchived).ToListAsync(ct);

    public async Task<Account> CreateAsync(Guid userId, string name, string type, string currency, CancellationToken ct)
    {
        var account = Account.Create(Guid.NewGuid(), userId, name, type, currency);
        db.Accounts.Add(account);
        await db.SaveChangesAsync(ct);
        return account;
    }

    public async Task<Account> GetAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Account not found");
        return account;
    }

    public async Task ArchiveAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var account = await GetAsync(userId, id, ct);
        account.Archive();
        await db.SaveChangesAsync(ct);
    }
}
