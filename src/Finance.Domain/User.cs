namespace Finance.Domain;

public sealed class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private User() { }

    public static User Create(Guid id, string email, string passwordHash)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id required", nameof(id));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email required", nameof(email));
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("PasswordHash required", nameof(passwordHash));

        var now = DateTime.UtcNow;
        return new User
        {
            Id = id,
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
