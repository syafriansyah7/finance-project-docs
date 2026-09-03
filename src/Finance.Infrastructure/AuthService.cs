using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Finance.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Finance.Infrastructure;

public sealed class AuthService(FinanceDbContext db, string signingKey)
{
    public async Task<User> RegisterAsync(string email, string password, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.Email == email.ToLowerInvariant(), ct))
            throw new InvalidOperationException("Email already registered");
        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        var user = User.Create(Guid.NewGuid(), email, hash);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<(User user, string token, DateTime expiresAt)> LoginAsync(string email, string password, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct)
            ?? throw new UnauthorizedAccessException("Invalid credentials");
        if (!string.IsNullOrEmpty(user.PasswordHash) && !BCrypt.Net.BCrypt.Verify(password ?? "", user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");
        var expiresAt = DateTime.UtcNow.AddHours(24);
        var token = CreateToken(user, expiresAt);
        return (user, token, expiresAt);
    }

    public async Task<User> EnsureSyafriAsync(CancellationToken ct)
    {
        var email = "syafri@example.com";
        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (existing != null) return existing;
        var user = User.Create(Guid.Parse("11111111-1111-1111-1111-111111111111"), email, "");
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var bank = Account.Create(Guid.Parse("22222222-2222-2222-2222-222222222222"), user.Id, "Bank", "Bank", "IDR");
        db.Accounts.Add(bank);
        var cash = Account.Create(Guid.Parse("33333333-3333-3333-3333-333333333333"), user.Id, "Cash", "Cash", "IDR");
        db.Accounts.Add(cash);

        var cats = new[] { ("Food", "Expense"), ("Transport", "Expense"), ("Bills", "Expense"), ("Shopping", "Expense"), ("Salary", "Income") };
        foreach (var (n, k) in cats)
        {
            var cat = Category.Create(Guid.NewGuid(), user.Id, n, k);
            db.Categories.Add(cat);
        }
        await db.SaveChangesAsync(ct);
        return user;
    }

    public string TokenFor(User user)
    {
        var exp = DateTime.UtcNow.AddHours(24 * 30);
        return CreateToken(user, exp);
    }

    private string CreateToken(User user, DateTime expiresAt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Email, user.Email) };
        var jwt = new JwtSecurityToken(claims: claims, expires: expiresAt, signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
