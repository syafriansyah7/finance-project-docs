using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Finance.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Finance.IntegrationTests;

public class Sprint1Tests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly InMemoryDatabaseRoot _root = new();
    private readonly WebApplicationFactory<Program> _factory;

    public Sprint1Tests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(b =>
        {
            b.ConfigureServices(s =>
            {
                var desc = s.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<FinanceDbContext>));
                if (desc != null) s.Remove(desc);
                s.AddDbContext<FinanceDbContext>(o => o.UseInMemoryDatabase("finance_test", _root));
            });
        });
    }

    private async Task<string> GetTokenAsync(string email = "test@example.com", string password = "Password123!")
    {
        var client = _factory.CreateClient();
        var reg = await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password });
        var regBody = await reg.Content.ReadAsStringAsync();
        if (!reg.IsSuccessStatusCode) throw new Exception($"register failed {reg.StatusCode} {regBody}");
        var res = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        var loginBody = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode) throw new Exception($"login failed {res.StatusCode} {loginBody}");
        var body = System.Text.Json.JsonSerializer.Deserialize<LoginResponse>(loginBody, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return body!.accessToken;
    }

    [Fact]
    public async Task Register_and_login_succeeds()
    {
        var client = _factory.CreateClient();
        var reg = await client.PostAsJsonAsync("/api/v1/auth/register", new { Email = "alice2@example.com", Password = "Password123!" });
        Assert.True(reg.IsSuccessStatusCode, await reg.Content.ReadAsStringAsync());
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { Email = "alice2@example.com", Password = "Password123!" });
        var body = await login.Content.ReadAsStringAsync();
        Assert.True(login.IsSuccessStatusCode, body);
    }

    [Fact]
    public async Task Accounts_require_auth()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/v1/accounts");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Create_account_and_list()
    {
        var token = await GetTokenAsync("bob@example.com");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await client.PostAsJsonAsync("/api/v1/accounts", new { name = "Cash", type = "Cash", currency = "IDR" });
        Assert.Equal(System.Net.HttpStatusCode.Created, create.StatusCode);

        var list = await client.GetAsync("/api/v1/accounts");
        list.EnsureSuccessStatusCode();
        var accounts = await list.Content.ReadFromJsonAsync<List<AccountDto>>();
        Assert.Single(accounts!);
    }

    [Fact]
    public async Task Duplicate_category_returns_409()
    {
        var token = await GetTokenAsync("cat@example.com");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var r1 = await client.PostAsJsonAsync("/api/v1/categories", new { name = "Food", kind = "Expense" });
        Assert.Equal(System.Net.HttpStatusCode.Created, r1.StatusCode);
        var r2 = await client.PostAsJsonAsync("/api/v1/categories", new { name = "Food", kind = "Expense" });
        Assert.Equal(System.Net.HttpStatusCode.Conflict, r2.StatusCode);
    }

    [Fact]
    public async Task Ownership_isolated()
    {
        var tokenA = await GetTokenAsync("ownerA@example.com");
        var tokenB = await GetTokenAsync("ownerB@example.com");

        var clientA = _factory.CreateClient();
        clientA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        await clientA.PostAsJsonAsync("/api/v1/accounts", new { name = "Cash", type = "Cash" });

        var clientB = _factory.CreateClient();
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var listB = await clientB.GetAsync("/api/v1/accounts");
        var accountsB = await listB.Content.ReadFromJsonAsync<List<AccountDto>>();
        Assert.Empty(accountsB!);
    }

    private record LoginResponse(string accessToken, DateTime expiresAt);
    private record AccountDto(Guid Id, string Name, string Type, string Currency, bool IsArchived);
}
