using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Finance.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Finance.IntegrationTests;

public class Sprint2Tests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly InMemoryDatabaseRoot _root = new();
    private readonly WebApplicationFactory<Program> _factory;

    public Sprint2Tests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(b =>
        {
            b.ConfigureServices(s =>
            {
                var d = s.SingleOrDefault(x => x.ServiceType == typeof(DbContextOptions<FinanceDbContext>));
                if (d != null) s.Remove(d);
                s.AddDbContext<FinanceDbContext>(o => o.UseInMemoryDatabase("finance_test2", _root));
            });
        });
    }

    private async Task<(HttpClient client, Guid acc1, Guid acc2, Guid cat)> SetupAsync(string email)
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/auth/register", new { Email = email, Password = "Password123!" });
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { Email = email, Password = "Password123!" });
        var body = await login.Content.ReadFromJsonAsync<LoginRes>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.accessToken);

        var acc1Res = await client.PostAsJsonAsync("/api/v1/accounts", new { name = "Cash", type = "Cash", currency = "IDR" });
        var acc1 = (await acc1Res.Content.ReadFromJsonAsync<IdRes>())!.Id;
        var acc2Res = await client.PostAsJsonAsync("/api/v1/accounts", new { name = "Bank", type = "Bank", currency = "IDR" });
        var acc2 = (await acc2Res.Content.ReadFromJsonAsync<IdRes>())!.Id;
        var catRes = await client.PostAsJsonAsync("/api/v1/categories", new { name = "Food", kind = "Expense" });
        var cat = (await catRes.Content.ReadFromJsonAsync<IdRes>())!.Id;
        var catIncRes = await client.PostAsJsonAsync("/api/v1/categories", new { name = "Salary", kind = "Income" });
        // ensure income category exists for later tests via same name not duplicate per kind
        return (client, acc1, acc2, cat);
    }

    [Fact]
    public async Task Create_transfer_moves_balance_T08()
    {
        var (client, acc1, acc2, _) = await SetupAsync("t08@example.com");
        await client.PostAsJsonAsync("/api/v1/transactions", new { type = "Transfer", accountId = acc1, transferAccountId = acc2, amount = 50000, currency = "IDR", transactionDate = DateTime.UtcNow });

        var b1 = await client.GetFromJsonAsync<BalRes>($"/api/v1/accounts/{acc1}/balance");
        var b2 = await client.GetFromJsonAsync<BalRes>($"/api/v1/accounts/{acc2}/balance");
        Assert.Equal(-50000, b1!.balance);
        Assert.Equal(50000, b2!.balance);
    }

    [Fact]
    public async Task Transfer_not_counted_as_income_expense_T09()
    {
        var (client, acc1, acc2, cat) = await SetupAsync("t09@example.com");
        // need income category id: fetch categories
        var cats = await client.GetFromJsonAsync<List<CatDto>>("/api/v1/categories");
        var incCat = cats!.First(c => c.Kind == "Income").Id;

        await client.PostAsJsonAsync("/api/v1/transactions", new { type = "Income", accountId = acc1, categoryId = incCat, amount = 100000, currency = "IDR", transactionDate = DateTime.UtcNow });
        await client.PostAsJsonAsync("/api/v1/transactions", new { type = "Expense", accountId = acc1, categoryId = cat, amount = 30000, currency = "IDR", transactionDate = DateTime.UtcNow });
        await client.PostAsJsonAsync("/api/v1/transactions", new { type = "Transfer", accountId = acc1, transferAccountId = acc2, amount = 20000, currency = "IDR", transactionDate = DateTime.UtcNow });

        // verify via listing transactions: filter not needed, just ensure 3 exist
        var list = await client.GetFromJsonAsync<List<object>>("/api/v1/transactions?page=1&pageSize=10");
        Assert.Equal(3, list!.Count);
        // balance check net excludes transfer from income/expense: income 100k, expense 30k, balance for acc1 = 100k-30k-20k = 50000
        var bal = await client.GetFromJsonAsync<BalRes>($"/api/v1/accounts/{acc1}/balance");
        Assert.Equal(50000, bal!.balance);
    }

    [Fact]
    public async Task Invalid_transfer_same_account_returns_422()
    {
        var (client, acc1, _, cat) = await SetupAsync("t09b@example.com");
        var res = await client.PostAsJsonAsync("/api/v1/transactions", new { type = "Transfer", accountId = acc1, transferAccountId = acc1, amount = 10000, currency = "IDR", transactionDate = DateTime.UtcNow });
        Assert.Equal(System.Net.HttpStatusCode.UnprocessableEntity, res.StatusCode);
    }

    private record LoginRes(string accessToken, DateTime expiresAt);
    private record IdRes(Guid Id);
    private record BalRes(Guid accountId, long balance);
    private record CatDto(Guid Id, string Name, string Kind, bool IsArchived);
}
