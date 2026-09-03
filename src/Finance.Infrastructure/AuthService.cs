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
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");
        var expiresAt = DateTime.UtcNow.AddHours(24);
        var token = CreateToken(user, expiresAt);
        return (user, token, expiresAt);
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
