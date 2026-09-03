using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Finance.Infrastructure;
using Finance.Mobile;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Finance.IntegrationTests;

public class Sprint3SyncTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly InMemoryDatabaseRoot _root = new();
    private readonly WebApplicationFactory<Program> _factory;

    public Sprint3SyncTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(b =>
        {
            b.ConfigureServices(s =>
            {
                var d = s.SingleOrDefault(x => x.ServiceType == typeof(DbContextOptions<FinanceDbContext>));
                if (d != null) s.Remove(d);
                s.AddDbContext<FinanceDbContext>(o => o.UseInMemoryDatabase("finance_sync", _root));
            });
        });
    }

    private async Task<(HttpClient client, Guid acc)> SetupAsync(string email)
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/auth/register", new { Email = email, Password = "Password123!" });
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { Email = email, Password = "Password123!" });
        var body = await login.Content.ReadFromJsonAsync<LoginRes>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.accessToken);
        var accRes = await client.PostAsJsonAsync("/api/v1/accounts", new { name = "Cash", type = "Cash", currency = "IDR" });
        var acc = (await accRes.Content.ReadFromJsonAsync<IdRes>())!.Id;
        return (client, acc);
    }

    [Fact]
    public async Task Push_idempotent_same_operationId_no_duplicate_T05()
    {
        var (client, acc) = await SetupAsync("sync5@example.com");
        var txId = Guid.NewGuid();
        var opId = Guid.NewGuid();
        var payload = new { type = "Expense", accountId = acc, categoryId = await GetOrCreateCategory(client), amount = 25000, currency = "IDR", transactionDate = DateTime.UtcNow };

        var item = new { operationId = opId, entity = "transaction", entityId = txId, operation = "create", clientUpdatedAt = DateTime.UtcNow, payload };
        var r1 = await client.PostAsJsonAsync("/api/v1/sync/push", new { items = new[] { item } });
        r1.EnsureSuccessStatusCode();
        var r2 = await client.PostAsJsonAsync("/api/v1/sync/push", new { items = new[] { item } });
        r2.EnsureSuccessStatusCode();

        var list = await client.GetFromJsonAsync<List<object>>("/api/v1/transactions?page=1&pageSize=10");
        Assert.Single(list!);
    }

    [Fact]
    public async Task Push_multiple_in_order_T07()
    {
        var (client, acc) = await SetupAsync("syncOrder@example.com");
        var cat = await GetOrCreateCategory(client);
        var items = Enumerable.Range(0, 3).Select(i => new
        {
            operationId = Guid.NewGuid(),
            entity = "transaction",
            entityId = Guid.NewGuid(),
            operation = "create",
            clientUpdatedAt = DateTime.UtcNow.AddSeconds(i),
            payload = new Dictionary<string, object?> { ["type"] = "Expense", ["accountId"] = acc, ["categoryId"] = cat, ["amount"] = 10000 + i, ["currency"] = "IDR", ["transactionDate"] = DateTime.UtcNow }
        }).ToArray();

        var res = await client.PostAsJsonAsync("/api/v1/sync/push", new { items });
        res.EnsureSuccessStatusCode();
        var list = await client.GetFromJsonAsync<List<object>>("/api/v1/transactions?page=1&pageSize=10");
        Assert.Equal(3, list!.Count);
    }

    [Fact]
    public async Task Pull_returns_changes_after_cursor()
    {
        var (client, acc) = await SetupAsync("syncPull@example.com");
        var cat = await GetOrCreateCategory(client);
        var txId = Guid.NewGuid();
        var opId = Guid.NewGuid();
        await client.PostAsJsonAsync("/api/v1/sync/push", new { items = new[] { new { operationId = opId, entity = "transaction", entityId = txId, operation = "create", clientUpdatedAt = DateTime.UtcNow, payload = new Dictionary<string, object?> { ["type"] = "Expense", ["accountId"] = acc, ["categoryId"] = cat, ["amount"] = 5000, ["currency"] = "IDR", ["transactionDate"] = DateTime.UtcNow } } } });

        var pull1 = await client.GetFromJsonAsync<PullRes>("/api/v1/sync/pull?cursor=0");
        Assert.Single(pull1!.changes);
        var pull2 = await client.GetFromJsonAsync<PullRes>($"/api/v1/sync/pull?cursor={pull1.cursor}");
        Assert.Empty(pull2!.changes);
    }

    [Fact]
    public async Task Queue_survives_and_retries()
    {
        var queue = new InMemorySyncQueue();
        var item = new SyncQueueItem { EntityType = "transaction", EntityId = Guid.NewGuid(), OperationType = "create", PayloadJson = "{}" };
        await queue.EnqueueAsync(item, CancellationToken.None);
        Assert.Equal(1, await queue.CountPendingAsync(CancellationToken.None));
        await queue.MarkFailedAsync(item.OperationId, "timeout", CancellationToken.None);
        var pending = await queue.GetPendingAsync(CancellationToken.None);
        Assert.Single(pending);
        Assert.Equal(SyncStatus.Failed, pending[0].Status);
        await queue.MarkSyncedAsync(item.OperationId, CancellationToken.None);
        Assert.Equal(0, await queue.CountPendingAsync(CancellationToken.None));
    }

    private async Task<Guid> GetOrCreateCategory(HttpClient client)
    {
        var cats = await client.GetFromJsonAsync<List<CatDto>>("/api/v1/categories");
        var existing = cats!.FirstOrDefault(c => c.Name == "Food");
        if (existing != null) return existing.Id;
        var res = await client.PostAsJsonAsync("/api/v1/categories", new { name = "Food", kind = "Expense" });
        return (await res.Content.ReadFromJsonAsync<IdRes>())!.Id;
    }

    private record LoginRes(string accessToken, DateTime expiresAt);
    private record IdRes(Guid Id);
    private record CatDto(Guid Id, string Name, string Kind, bool IsArchived);
    private record PullRes(string cursor, List<object> changes);
}
