using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Finance.Infrastructure;
using Finance.Mobile;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text.Json;

namespace Finance.IntegrationTests;

public class Sprint5OfflineE2ETest : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly InMemoryDatabaseRoot _root = new();
    private readonly WebApplicationFactory<Program> _factory;

    public Sprint5OfflineE2ETest(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(b =>
        {
            b.ConfigureServices(s =>
            {
                var d = s.SingleOrDefault(x => x.ServiceType == typeof(DbContextOptions<FinanceDbContext>));
                if (d != null) s.Remove(d);
                s.AddDbContext<FinanceDbContext>(o => o.UseInMemoryDatabase("finance_e2e", _root));
            });
        });
    }

    [Fact]
    public async Task Offline_create_restart_sync_no_duplicate_T02_T06()
    {
        // Step 1-3: online create account/category
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/auth/register", new { Email = "e2e@example.com", Password = "Password123!" });
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { Email = "e2e@example.com", Password = "Password123!" });
        var body = await login.Content.ReadFromJsonAsync<LoginRes>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.accessToken);

        var acc = (await (await client.PostAsJsonAsync("/api/v1/accounts", new { name = "Cash", type = "Cash" })).Content.ReadFromJsonAsync<IdRes>())!.Id;
        var cat = (await (await client.PostAsJsonAsync("/api/v1/categories", new { name = "Food", kind = "Expense" })).Content.ReadFromJsonAsync<IdRes>())!.Id;

        // Step 4-6: offline - enqueue 3 transactions locally (simulated queue)
        var queue = new InMemorySyncQueue();
        var txIds = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray();
        foreach (var txId in txIds)
        {
            var payload = JsonSerializer.Serialize(new { type = "Expense", accountId = acc, categoryId = cat, amount = 25000, currency = "IDR", transactionDate = DateTime.UtcNow });
            await queue.EnqueueAsync(new SyncQueueItem { OperationId = Guid.NewGuid(), EntityType = "transaction", EntityId = txId, OperationType = "create", PayloadJson = payload }, CancellationToken.None);
        }
        Assert.Equal(3, await queue.CountPendingAsync(CancellationToken.None));

        // Step 6: close/reopen app - queue survives (InMemorySyncQueue simulates persistence; real SQLite would)
        var pending = await queue.GetPendingAsync(CancellationToken.None);
        Assert.Equal(3, pending.Count);

        // Step 7-9: online - push queue in order
        var items = pending.Select(p => new
        {
            operationId = p.OperationId,
            entity = p.EntityType,
            entityId = p.EntityId,
            operation = p.OperationType,
            clientUpdatedAt = p.CreatedAt,
            payload = JsonSerializer.Deserialize<Dictionary<string, object?>>(p.PayloadJson)
        }).ToArray();

        var push = await client.PostAsJsonAsync("/api/v1/sync/push", new { items });
        push.EnsureSuccessStatusCode();
        foreach (var p in pending) await queue.MarkSyncedAsync(p.OperationId, CancellationToken.None);
        Assert.Equal(0, await queue.CountPendingAsync(CancellationToken.None));

        // verify exactly 3 transactions, no duplicates
        var list = await client.GetFromJsonAsync<List<object>>("/api/v1/transactions?page=1&pageSize=10");
        Assert.Equal(3, list!.Count);

        // retry same push should not duplicate (T05)
        var retry = await client.PostAsJsonAsync("/api/v1/sync/push", new { items });
        retry.EnsureSuccessStatusCode();
        var list2 = await client.GetFromJsonAsync<List<object>>("/api/v1/transactions?page=1&pageSize=10");
        Assert.Equal(3, list2!.Count);
    }

    private record LoginRes(string accessToken, DateTime expiresAt);
    private record IdRes(Guid Id);
}
