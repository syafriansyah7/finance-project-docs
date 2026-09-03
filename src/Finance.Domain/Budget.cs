namespace Finance.Domain;

public sealed class Budget
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid CategoryId { get; private set; }
    public DateTime Month { get; private set; }
    public long Amount { get; private set; }
    public string Currency { get; private set; } = "IDR";
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Budget() { }

    public static Budget Create(Guid id, Guid userId, Guid categoryId, DateTime month, long amount, string currency = "IDR")
    {
        if (id == Guid.Empty) throw new ArgumentException("Id required", nameof(id));
        if (userId == Guid.Empty) throw new ArgumentException("UserId required", nameof(userId));
        if (categoryId == Guid.Empty) throw new ArgumentException("CategoryId required", nameof(categoryId));
        if (amount < 0) throw new ArgumentException("Amount must be >=0", nameof(amount));
        if (currency.Length != 3) throw new ArgumentException("Currency must be 3 chars", nameof(currency));

        var m = new DateTime(month.Year, month.Month, 25, 0, 0, 0, DateTimeKind.Utc);
        if (month.Day < 25) m = m.AddMonths(-1);
        return new Budget
        {
            Id = id,
            UserId = userId,
            CategoryId = categoryId,
            Month = m,
            Amount = amount,
            Currency = currency.ToUpperInvariant(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateAmount(long amount)
    {
        if (amount < 0) throw new ArgumentException("Amount must be >=0", nameof(amount));
        Amount = amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public (long spent, long remaining, double pct) Progress(IEnumerable<Transaction> transactions)
    {
        var start = new DateTime(Month.Year, Month.Month, 25, 0, 0, 0, DateTimeKind.Utc);
        if (Month.Day < 25) start = start.AddMonths(-1);
        var end = start.AddMonths(1).AddTicks(-1);
        var spent = transactions.Where(t => t.CategoryId == CategoryId && t.Type == TransactionType.Expense && t.DeletedAt == null
            && t.TransactionDate >= start && t.TransactionDate <= end).Sum(t => t.Amount);
        var remaining = Amount - spent;
        var pct = Amount == 0 ? 0 : (double)spent / Amount * 100;
        return (spent, remaining, pct);
    }
}
