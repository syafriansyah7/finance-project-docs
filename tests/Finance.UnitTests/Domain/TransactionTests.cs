using Finance.Domain;

namespace Finance.UnitTests.Domain;

public class TransactionTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid Acc1 = Guid.NewGuid();
    private static readonly Guid Acc2 = Guid.NewGuid();
    private static readonly Guid Cat = Guid.NewGuid();

    [Fact]
    public void Income_requires_category()
    {
        Assert.Throws<ArgumentException>(() => Transaction.Create(Guid.NewGuid(), UserId, TransactionType.Income, Acc1, null, null, 10000, "IDR", null, DateTime.UtcNow));
    }

    [Fact]
    public void Expense_requires_category()
    {
        Assert.Throws<ArgumentException>(() => Transaction.Create(Guid.NewGuid(), UserId, TransactionType.Expense, Acc1, null, null, 5000, "IDR", null, DateTime.UtcNow));
    }

    [Fact]
    public void Transfer_requires_destination_and_no_category()
    {
        Assert.Throws<ArgumentException>(() => Transaction.Create(Guid.NewGuid(), UserId, TransactionType.Transfer, Acc1, null, Cat, 10000, "IDR", null, DateTime.UtcNow));
        Assert.Throws<ArgumentException>(() => Transaction.Create(Guid.NewGuid(), UserId, TransactionType.Transfer, Acc1, Acc1, null, 10000, "IDR", null, DateTime.UtcNow));
        var ok = Transaction.Create(Guid.NewGuid(), UserId, TransactionType.Transfer, Acc1, Acc2, null, 10000, "IDR", null, DateTime.UtcNow);
        Assert.Equal(TransactionType.Transfer, ok.Type);
    }

    [Fact]
    public void Amount_must_be_positive()
    {
        Assert.Throws<ArgumentException>(() => Transaction.Create(Guid.NewGuid(), UserId, TransactionType.Expense, Acc1, null, Cat, 0, "IDR", null, DateTime.UtcNow));
    }

    [Fact]
    public void Balance_transfer_moves_between_accounts()
    {
        var tx = Transaction.Create(Guid.NewGuid(), UserId, TransactionType.Transfer, Acc1, Acc2, null, 50000, "IDR", null, DateTime.UtcNow);
        Assert.Equal(-50000, BalanceCalculator.Calculate(Acc1, new[] { tx }));
        Assert.Equal(50000, BalanceCalculator.Calculate(Acc2, new[] { tx }));
    }

    [Fact]
    public void Summary_excludes_transfer()
    {
        var income = Transaction.Create(Guid.NewGuid(), UserId, TransactionType.Income, Acc1, null, Cat, 100000, "IDR", null, DateTime.UtcNow);
        var expense = Transaction.Create(Guid.NewGuid(), UserId, TransactionType.Expense, Acc1, null, Cat, 30000, "IDR", null, DateTime.UtcNow);
        var transfer = Transaction.Create(Guid.NewGuid(), UserId, TransactionType.Transfer, Acc1, Acc2, null, 20000, "IDR", null, DateTime.UtcNow);
        var (inc, exp, net) = BalanceCalculator.Summary(new[] { income, expense, transfer });
        Assert.Equal(100000, inc);
        Assert.Equal(30000, exp);
        Assert.Equal(70000, net);
    }

    [Fact]
    public void Deleted_transactions_excluded_from_balance()
    {
        var tx = Transaction.Create(Guid.NewGuid(), UserId, TransactionType.Income, Acc1, null, Cat, 10000, "IDR", null, DateTime.UtcNow);
        tx.SoftDelete();
        Assert.Equal(0, BalanceCalculator.Calculate(Acc1, new[] { tx }));
    }
}
