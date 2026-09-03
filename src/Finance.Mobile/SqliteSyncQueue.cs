using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace Finance.Mobile;

public sealed class SqliteSyncQueue : ISyncQueue, IAsyncDisposable
{
    private readonly SqliteConnection _conn;

    public SqliteSyncQueue(string path = "finance.db")
    {
        _conn = new SqliteConnection($"Data Source={path}");
        _conn.Open();
        Init();
    }

    private void Init()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS sync_queue (
                operation_id TEXT PRIMARY KEY,
                entity_type TEXT NOT NULL,
                entity_id TEXT NOT NULL,
                operation_type TEXT NOT NULL,
                payload_json TEXT NOT NULL,
                status TEXT NOT NULL,
                attempt_count INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL,
                last_attempt_at TEXT,
                last_error TEXT
            );";
        cmd.ExecuteNonQuery();
    }

    public async Task EnqueueAsync(SyncQueueItem item, CancellationToken ct)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "INSERT INTO sync_queue (operation_id, entity_type, entity_id, operation_type, payload_json, status, attempt_count, created_at) VALUES ($id,$type,$eid,$op,$payload,$status,$cnt,$created)";
        cmd.Parameters.AddWithValue("$id", item.OperationId.ToString());
        cmd.Parameters.AddWithValue("$type", item.EntityType);
        cmd.Parameters.AddWithValue("$eid", item.EntityId.ToString());
        cmd.Parameters.AddWithValue("$op", item.OperationType);
        cmd.Parameters.AddWithValue("$payload", item.PayloadJson);
        cmd.Parameters.AddWithValue("$status", item.Status.ToString());
        cmd.Parameters.AddWithValue("$cnt", item.AttemptCount);
        cmd.Parameters.AddWithValue("$created", item.CreatedAt.ToString("O"));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<SyncQueueItem>> GetPendingAsync(CancellationToken ct)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT operation_id, entity_type, entity_id, operation_type, payload_json, status, attempt_count, created_at, last_attempt_at, last_error FROM sync_queue WHERE status IN ('Pending','Failed') ORDER BY created_at";
        using var reader = await cmd.ExecuteReaderAsync(ct);
        var list = new List<SyncQueueItem>();
        while (await reader.ReadAsync(ct))
        {
            list.Add(new SyncQueueItem
            {
                OperationId = Guid.Parse(reader.GetString(0)),
                EntityType = reader.GetString(1),
                EntityId = Guid.Parse(reader.GetString(2)),
                OperationType = reader.GetString(3),
                PayloadJson = reader.GetString(4),
                Status = Enum.Parse<SyncStatus>(reader.GetString(5)),
                AttemptCount = reader.GetInt32(6),
                CreatedAt = DateTime.Parse(reader.GetString(7)),
                LastAttemptAt = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8)),
                LastError = reader.IsDBNull(9) ? null : reader.GetString(9)
            });
        }
        return list;
    }

    public async Task MarkSyncedAsync(Guid operationId, CancellationToken ct)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "UPDATE sync_queue SET status='Synced' WHERE operation_id=$id";
        cmd.Parameters.AddWithValue("$id", operationId.ToString());
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task MarkFailedAsync(Guid operationId, string error, CancellationToken ct)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "UPDATE sync_queue SET status='Failed', last_error=$err, attempt_count=attempt_count+1, last_attempt_at=$now WHERE operation_id=$id";
        cmd.Parameters.AddWithValue("$id", operationId.ToString());
        cmd.Parameters.AddWithValue("$err", error);
        cmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O"));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<int> CountPendingAsync(CancellationToken ct)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sync_queue WHERE status IN ('Pending','Failed')";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
    }

    public async ValueTask DisposeAsync()
    {
        await _conn.CloseAsync();
        _conn.Dispose();
    }
}
