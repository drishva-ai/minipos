using MassTransit;
using MiniPOS.Shared.Messages;
using Payment.Service.Services;

namespace Payment.Service.Consumers;

/// <summary>
/// Consumes ProcessPaymentCommand from payment-queue.
///
/// Flow:
///   1. Simulate EFT terminal for Card / Contactless (real delay + auth code)
///   2. Generate transaction ID
///   3. Save to transaction repository (in-memory; SQL Server in Phase 2)
///   4. Publish PaymentCompletedEvent → pos-webhost-events → POS.WebHost → SignalR → React
/// </summary>
public sealed class ProcessPaymentConsumer : IConsumer<ProcessPaymentCommand>
{
    private readonly ITransactionRepository          _repo;
    private readonly IPublishEndpoint                _bus;
    private readonly ILogger<ProcessPaymentConsumer> _log;

    public ProcessPaymentConsumer(
        ITransactionRepository repo,
        IPublishEndpoint bus,
        ILogger<ProcessPaymentConsumer> log)
    {
        _repo = repo;
        _bus  = bus;
        _log  = log;
    }

    public async Task Consume(ConsumeContext<ProcessPaymentCommand> ctx)
    {
        var cmd = ctx.Message;
        _log.LogInformation("[ProcessPayment] BasketId: {B} | Method: {M} | £{T:F2}",
            cmd.BasketId, cmd.PaymentMethod, cmd.Total);

        // 1. Simulate terminal interaction for electronic payments
        string? authCode = null;
        if (cmd.PaymentMethod is "Card" or "Contactless")
        {
            _log.LogInformation("[EFT] Connecting to PIN pad — {M} terminal...", cmd.PaymentMethod);
            await Task.Delay(800, ctx.CancellationToken);
            authCode = Random.Shared.Next(100_000, 999_999).ToString();
            _log.LogInformation("[EFT] Authorization APPROVED — AuthCode: {Code}", authCode);
        }

        // 2. Generate unique transaction ID
        var txnId = $"TXN-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        // 3. Persist transaction
        var record = new TransactionRecord
        {
            TransactionId = txnId,
            BasketId      = cmd.BasketId,
            CashierId     = cmd.CashierId,
            PaymentMethod = cmd.PaymentMethod,
            Total         = cmd.Total,
            ItemCount     = cmd.Items.Count,
            AuthCode      = authCode,
            CompletedAt   = DateTime.UtcNow
        };
        await _repo.SaveAsync(record);
        _log.LogInformation("[Repo] Saved transaction {TxnId} — £{T:F2}", txnId, cmd.Total);

        // 4. Notify POS.WebHost → React via SignalR
        await _bus.Publish(new PaymentCompletedEvent
        {
            BasketId      = cmd.BasketId,
            TransactionId = txnId,
            Total         = cmd.Total,
            PaymentMethod = cmd.PaymentMethod,
            Status        = "Approved",
            AuthCode      = authCode,
            CompletedAt   = DateTime.UtcNow
        });

        _log.LogInformation("[ProcessPayment] Complete — TxnId: {TxnId}", txnId);
    }
}
