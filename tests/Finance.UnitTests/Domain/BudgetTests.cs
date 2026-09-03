using Finance.Domain;

namespace Finance.UnitTests.Domain;

public class BudgetTests
{
    [Fact]
    public void Progress_calculates_spent_remaining_pct()
    {
        var cat = Guid.NewGuid();
        var user = Guid.NewGuid();
        var acc = Guid.NewGuid();
        var budget = Budget.Create(Guid.NewGuid(), user, cat, new DateTime(2026, 9, 1), 1500000);
        var tx1 = Transaction.Create(Guid.NewGuid(), user, TransactionType.Expense, acc, null, cat, 900000, "IDR", null, new DateTime(2026, 9, 10));
        var tx2 = Transaction.Create(Guid.NewGuid(), user, TransactionType.Expense, acc, null, cat, 100000, "IDR", null, new DateTime(2026, 8, 10));
        var (spent, remaining, pct) = budget.Progress(new[] { tx1, tx2 });
        Assert.Equal(900000, spent);
        Assert.Equal(600000, remaining);
        Assert.Equal(60, pct, 0);
    }

    [Fact]
    public void Create_rejects_negative_amount()
    {
        Assert.Throws<ArgumentException>(() => Budget.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, -1));
    }
}
