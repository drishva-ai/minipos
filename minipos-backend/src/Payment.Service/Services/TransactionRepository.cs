namespace Payment.Service.Services;

public sealed class TransactionRecord
{
    public required string  TransactionId { get; init; }
    public required string  BasketId      { get; init; }
    public required string  CashierId     { get; init; }
    public required string  PaymentMethod { get; init; }
    public required decimal Total         { get; init; }
    public required int     ItemCount     { get; init; }
    public string?  AuthCode    { get; init; }
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;
}

public interface ITransactionRepository
{
    Task SaveAsync(TransactionRecord record);
    Task<IReadOnlyList<TransactionRecord>> GetAllAsync();
    Task<TransactionRecord?> GetByIdAsync(string transactionId);
}

/// <summary>
/// In-memory store for demo.
/// Phase 2: replace with EF Core + SQL Server.
/// services.AddDbContext&lt;PaymentDbContext&gt;(o =&gt; o.UseSqlServer(connectionString));
/// </summary>
public sealed class InMemoryTransactionRepository : ITransactionRepository
{
    private readonly List<TransactionRecord> _store = [];
    private readonly object _lock = new();   // 'Lock' type requires .NET 9 — use object instead

    public Task SaveAsync(TransactionRecord record)
    {
        lock (_lock) { _store.Add(record); }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TransactionRecord>> GetAllAsync()
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<TransactionRecord>>(
                _store.OrderByDescending(t => t.CompletedAt).ToList());
        }
    }

    public Task<TransactionRecord?> GetByIdAsync(string txnId)
    {
        lock (_lock)
        {
            return Task.FromResult(_store.FirstOrDefault(t => t.TransactionId == txnId));
        }
    }
}
