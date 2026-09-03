using Finance.Domain;
using Microsoft.EntityFrameworkCore;

namespace Finance.Infrastructure;

public sealed class BudgetService(FinanceDbContext db)
{
    public async Task<Budget> UpsertAsync(Guid userId, Guid categoryId, DateTime month, long amount, string currency, CancellationToken ct)
    {
        var m = new DateTime(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var existing = await db.Budgets.FirstOrDefaultAsync(b => b.UserId == userId && b.CategoryId == categoryId && b.Month == m, ct);
        if (existing != null) { existing.UpdateAmount(amount); await db.SaveChangesAsync(ct); return existing; }

        var catExists = await db.Categories.AnyAsync(c => c.Id == categoryId && c.UserId == userId, ct);
        if (!catExists) throw new KeyNotFoundException("Category not found");

        var budget = Budget.Create(Guid.NewGuid(), userId, categoryId, m, amount, currency);
        db.Budgets.Add(budget);
        await db.SaveChangesAsync(ct);
        return budget;
    }

    public async Task<IReadOnlyList<Budget>> ListAsync(Guid userId, DateTime month, CancellationToken ct)
    {
        var m = new DateTime(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return await db.Budgets.Where(b => b.UserId == userId && b.Month == m).ToListAsync(ct);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var b = await db.Budgets.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct) ?? throw new KeyNotFoundException("Budget not found");
        db.Budgets.Remove(b);
        await db.SaveChangesAsync(ct);
    }
}
