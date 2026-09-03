namespace Finance.Domain;

public sealed class SyncOperation
{
    public Guid OperationId { get; private set; }
    public Guid UserId { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string OperationType { get; private set; } = string.Empty;
    public DateTime ProcessedAt { get; private set; }
    public long ServerVersion { get; private set; }

    private SyncOperation() { }

    public static SyncOperation Create(Guid operationId, Guid userId, string entityType, Guid entityId, string operationType, long serverVersion)
    {
        if (operationId == Guid.Empty) throw new ArgumentException("OperationId required", nameof(operationId));
        return new SyncOperation
        {
            OperationId = operationId,
            UserId = userId,
            EntityType = entityType,
            EntityId = entityId,
            OperationType = operationType,
            ProcessedAt = DateTime.UtcNow,
            ServerVersion = serverVersion
        };
    }
}
