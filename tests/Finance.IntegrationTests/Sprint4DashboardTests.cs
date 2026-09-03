using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Finance.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Finance.IntegrationTests;

public class Sprint4DashboardTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly InMemoryDatabaseRoot _root = new();
    private readonly WebApplicationFactory<Program> _factory;

    public Sprint4DashboardTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(b =>
        {
            b.ConfigureServices(s =>
            {
                var d = s.SingleOrDefault(x => x.ServiceType == typeof(DbContextOptions<FinanceDbContext>));
                if (d != null) s.Remove(d);
                s.AddDbContext<FinanceDbContext>(o => o.UseInMemoryDatabase("finance_dash", _root));
            });
        });
    }

    [Fact]
    public async Task Dashboard_summary_T10()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/auth/register", new { Email = "dash@example.com", Password = "Password123!" });
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { Email = "dash@example.com", Password = "Password123!" });
        var body = await login.Content.ReadFromJsonAsync<LoginRes>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.accessToken);

        var acc = (await (await client.PostAsJsonAsync("/api/v1/accounts", new { name = "Cash", type = "Cash" })).Content.ReadFromJsonAsync<IdRes>())!.Id;
        var catInc = (await (await client.PostAsJsonAsync("/api/v1/categories", new { name = "Salary", kind = "Income" })).Content.ReadFromJsonAsync<IdRes>())!.Id;
        var catExp = (await (await client.PostAsJsonAsync("/api/v1/categories", new { name = "Food", kind = "Expense" })).Content.ReadFromJsonAsync<IdRes>())!.Id;

        var month = DateTime.UtcNow.ToString("yyyy-MM");
        var date = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 26, 12, 0, 0, DateTimeKind.Utc);
        await client.PostAsJsonAsync("/api/v1/transactions", new { type = "Income", accountId = acc, categoryId = catInc, amount = 10000000, currency = "IDR", transactionDate = date });
        await client.PostAsJsonAsync("/api/v1/transactions", new { type = "Expense", accountId = acc, categoryId = catExp, amount = 3250000, currency = "IDR", transactionDate = date });

        var dash = await client.GetFromJsonAsync<DashRes>($"/api/v1/dashboard/summary?period={month}");
        Assert.NotNull(dash);
        Assert.Equal(10000000, dash!.Income);
        Assert.Equal(3250000, dash.Expense);
        Assert.Equal(6750000, dash.Net);
        Assert.Single(dash.CategoryExpenses);
        Assert.Equal("Food", dash.CategoryExpenses[0].CategoryName);
    }

    private record LoginRes(string accessToken, DateTime expiresAt);
    private record IdRes(Guid Id);
    private record DashRes(string Month, long Income, long Expense, long Net, List<AccDto> Accounts, List<CatExpDto> CategoryExpenses, List<object> RecentTransactions);
    private record AccDto(Guid Id, string Name, long Balance);
    private record CatExpDto(Guid CategoryId, string CategoryName, long Amount);
}
