namespace Finance.Domain;

public sealed class Account
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public string Currency { get; private set; } = "IDR";
    public bool IsArchived { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private static readonly HashSet<string> ValidTypes = new(StringComparer.OrdinalIgnoreCase) { "Cash", "Bank", "EWallet" };

    private Account() { }

    public static Account Create(Guid id, Guid userId, string name, string type, string currency = "IDR")
    {
        if (id == Guid.Empty) throw new ArgumentException("Id required", nameof(id));
        if (userId == Guid.Empty) throw new ArgumentException("UserId required", nameof(userId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required", nameof(name));
        if (!ValidTypes.Contains(type)) throw new ArgumentException($"Type must be one of {string.Join(", ", ValidTypes)}", nameof(type));
        if (currency.Length != 3) throw new ArgumentException("Currency must be 3 chars", nameof(currency));

        var now = DateTime.UtcNow;
        return new Account
        {
            Id = id,
            UserId = userId,
            Name = name.Trim(),
            Type = type,
            Currency = currency.ToUpperInvariant(),
            IsArchived = false,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Archive()
    {
        IsArchived = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required", nameof(name));
        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
