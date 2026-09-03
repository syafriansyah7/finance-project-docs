using Finance.Domain;
using Microsoft.EntityFrameworkCore;

namespace Finance.Infrastructure;

public sealed class DashboardService(FinanceDbContext db)
{
    public async Task<DashboardSummary> SummaryAsync(Guid userId, DateTime month, CancellationToken ct)
    {
        var start = new DateTime(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1).AddTicks(-1);

        var txs = await db.Transactions.Where(t => t.UserId == userId && t.DeletedAt == null && t.TransactionDate >= start && t.TransactionDate <= end).ToListAsync(ct);
        var (income, expense, net) = BalanceCalculator.Summary(txs);

        var accounts = await db.Accounts.Where(a => a.UserId == userId && !a.IsArchived).ToListAsync(ct);
        var allTxs = await db.Transactions.Where(t => t.UserId == userId && t.DeletedAt == null).ToListAsync(ct);

        var accountBalances = accounts.Select(a => new AccountBalance(a.Id, a.Name, BalanceCalculator.Calculate(a.Id, allTxs))).ToList();

        var categoryExpenses = await db.Transactions
            .Where(t => t.UserId == userId && t.DeletedAt == null && t.Type == TransactionType.Expense && t.TransactionDate >= start && t.TransactionDate <= end && t.CategoryId != null)
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key!.Value, Amount = g.Sum(t => t.Amount) })
            .ToListAsync(ct);

        var catNames = await db.Categories.Where(c => c.UserId == userId).ToDictionaryAsync(c => c.Id, c => c.Name, ct);
        var categoryDtos = categoryExpenses.Select(c => new CategoryExpense(c.CategoryId, catNames.TryGetValue(c.CategoryId, out var n) ? n : "Unknown", c.Amount)).ToList();

        var recent = await db.Transactions.Where(t => t.UserId == userId && t.DeletedAt == null).OrderByDescending(t => t.TransactionDate).Take(10).ToListAsync(ct);

        return new DashboardSummary($"{month:yyyy-MM}", income, expense, net, accountBalances, categoryDtos, recent);
    }
}

public record AccountBalance(Guid Id, string Name, long Balance);
public record CategoryExpense(Guid CategoryId, string CategoryName, long Amount);
public record DashboardSummary(string Month, long Income, long Expense, long Net, IReadOnlyList<AccountBalance> Accounts, IReadOnlyList<CategoryExpense> CategoryExpenses, IReadOnlyList<Transaction> RecentTransactions);
