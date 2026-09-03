namespace Finance.Domain;

public sealed class Category
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Kind { get; private set; } = string.Empty;
    public bool IsArchived { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private static readonly HashSet<string> ValidKinds = new(StringComparer.OrdinalIgnoreCase) { "Income", "Expense" };

    private Category() { }

    public static Category Create(Guid id, Guid userId, string name, string kind)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id required", nameof(id));
        if (userId == Guid.Empty) throw new ArgumentException("UserId required", nameof(userId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required", nameof(name));
        if (!ValidKinds.Contains(kind)) throw new ArgumentException("Kind must be Income or Expense", nameof(kind));

        var now = DateTime.UtcNow;
        return new Category
        {
            Id = id,
            UserId = userId,
            Name = name.Trim(),
            Kind = kind,
            IsArchived = false,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required", nameof(name));
        Name = name.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        IsArchived = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
