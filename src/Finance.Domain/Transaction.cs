namespace Finance.Domain;

public enum TransactionType { Income, Expense, Transfer }

public sealed class Transaction
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public TransactionType Type { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid? TransferAccountId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public long Amount { get; private set; }
    public string Currency { get; private set; } = "IDR";
    public string? Description { get; private set; }
    public DateTime TransactionDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private Transaction() { }

    public static Transaction Create(
        Guid id, Guid userId, TransactionType type, Guid accountId,
        Guid? transferAccountId, Guid? categoryId, long amount,
        string currency, string? description, DateTime transactionDate)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id required", nameof(id));
        if (userId == Guid.Empty) throw new ArgumentException("UserId required", nameof(userId));
        if (accountId == Guid.Empty) throw new ArgumentException("AccountId required", nameof(accountId));
        if (amount <= 0) throw new ArgumentException("Amount must be > 0", nameof(amount));
        if (currency.Length != 3) throw new ArgumentException("Currency must be 3 chars", nameof(currency));

        if (type == TransactionType.Transfer)
        {
            if (transferAccountId is null) throw new ArgumentException("Transfer requires destination account", nameof(transferAccountId));
            if (transferAccountId == accountId) throw new ArgumentException("Transfer source and destination must differ");
            if (categoryId is not null) throw new ArgumentException("Transfer must not have category");
        }
        else
        {
            if (categoryId is null) throw new ArgumentException($"{type} requires category");
            if (transferAccountId is not null) throw new ArgumentException($"{type} must not have transfer account");
        }

        var now = DateTime.UtcNow;
        return new Transaction
        {
            Id = id,
            UserId = userId,
            Type = type,
            AccountId = accountId,
            TransferAccountId = transferAccountId,
            CategoryId = categoryId,
            Amount = amount,
            Currency = currency.ToUpperInvariant(),
            Description = description,
            TransactionDate = transactionDate.ToUniversalTime(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void SoftDelete()
    {
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
