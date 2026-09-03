namespace Finance.Mobile;

public enum SyncStatus { Pending, Sending, Failed, Synced }

public sealed class SyncQueueItem
{
    public Guid OperationId { get; init; } = Guid.NewGuid();
    public string EntityType { get; init; } = string.Empty;
    public Guid EntityId { get; init; }
    public string OperationType { get; init; } = string.Empty;
    public string PayloadJson { get; init; } = "{}";
    public SyncStatus Status { get; set; } = SyncStatus.Pending;
    public int AttemptCount { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? LastAttemptAt { get; set; }
    public string? LastError { get; set; }
}

public interface ISyncQueue
{
    Task EnqueueAsync(SyncQueueItem item, CancellationToken ct);
    Task<IReadOnlyList<SyncQueueItem>> GetPendingAsync(CancellationToken ct);
    Task MarkSyncedAsync(Guid operationId, CancellationToken ct);
    Task MarkFailedAsync(Guid operationId, string error, CancellationToken ct);
    Task<int> CountPendingAsync(CancellationToken ct);
}

public sealed class InMemorySyncQueue : ISyncQueue
{
    private readonly List<SyncQueueItem> _items = new();
    private readonly object _lock = new();

    public Task EnqueueAsync(SyncQueueItem item, CancellationToken ct)
    {
        lock (_lock) _items.Add(item);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SyncQueueItem>> GetPendingAsync(CancellationToken ct)
    {
        lock (_lock) return Task.FromResult<IReadOnlyList<SyncQueueItem>>(_items.Where(i => i.Status == SyncStatus.Pending || i.Status == SyncStatus.Failed).OrderBy(i => i.CreatedAt).ToList());
    }

    public Task MarkSyncedAsync(Guid operationId, CancellationToken ct)
    {
        lock (_lock)
        {
            var item = _items.FirstOrDefault(i => i.OperationId == operationId);
            if (item != null) item.Status = SyncStatus.Synced;
        }
        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(Guid operationId, string error, CancellationToken ct)
    {
        lock (_lock)
        {
            var item = _items.FirstOrDefault(i => i.OperationId == operationId);
            if (item != null) { item.Status = SyncStatus.Failed; item.LastError = error; item.AttemptCount++; item.LastAttemptAt = DateTime.UtcNow; }
        }
        return Task.CompletedTask;
    }

    public Task<int> CountPendingAsync(CancellationToken ct)
    {
        lock (_lock) return Task.FromResult(_items.Count(i => i.Status == SyncStatus.Pending || i.Status == SyncStatus.Failed));
    }
}
