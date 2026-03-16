using MassTransit;
using Microsoft.AspNetCore.SignalR;
using MiniPOS.Shared.Messages;
using POS.WebHost.Hubs;

namespace POS.WebHost.Consumers;

/// <summary>
/// Receives BasketUpdatedEvent from Basket.Service (via pos-webhost-events queue).
/// Pushes updated basket to ALL connected React clients via SignalR.
/// </summary>
public sealed class BasketUpdatedConsumer : IConsumer<BasketUpdatedEvent>
{
    private readonly IHubContext<PosHub>         _hub;
    private readonly ILogger<BasketUpdatedConsumer> _log;

    public BasketUpdatedConsumer(IHubContext<PosHub> hub, ILogger<BasketUpdatedConsumer> log)
    {
        _hub = hub; _log = log;
    }

    public async Task Consume(ConsumeContext<BasketUpdatedEvent> ctx)
    {
        var ev = ctx.Message;
        _log.LogInformation("[Consumer:BasketUpdated] Basket: {B} | Items: {N} | Total: £{T:F2}",
            ev.BasketId, ev.Items.Count, ev.Total);

        await _hub.Clients.All.SendAsync("BasketUpdated", new
        {
            basketId  = ev.BasketId,
            cashierId = ev.CashierId,
            items     = ev.Items,
            total     = ev.Total,
            updatedAt = ev.UpdatedAt
        });
    }
}

/// <summary>
/// Receives PaymentCompletedEvent from Payment.Service.
/// Pushes receipt/confirmation to connected React clients.
/// </summary>
public sealed class PaymentCompletedConsumer : IConsumer<PaymentCompletedEvent>
{
    private readonly IHubContext<PosHub>           _hub;
    private readonly ILogger<PaymentCompletedConsumer> _log;

    public PaymentCompletedConsumer(IHubContext<PosHub> hub, ILogger<PaymentCompletedConsumer> log)
    {
        _hub = hub; _log = log;
    }

    public async Task Consume(ConsumeContext<PaymentCompletedEvent> ctx)
    {
        var ev = ctx.Message;
        _log.LogInformation("[Consumer:PaymentCompleted] TxnId: {T} | £{A:F2} | {M}",
            ev.TransactionId, ev.Total, ev.PaymentMethod);

        await _hub.Clients.All.SendAsync("PaymentCompleted", new
        {
            basketId      = ev.BasketId,
            transactionId = ev.TransactionId,
            total         = ev.Total,
            paymentMethod = ev.PaymentMethod,
            status        = ev.Status,
            authCode      = ev.AuthCode,
            completedAt   = ev.CompletedAt
        });
    }
}

/// <summary>
/// Receives PumpStatusChangedEvent from Forecourt.Service.
/// Pushes live pump state to React — no polling needed.
/// </summary>
public sealed class PumpStatusChangedConsumer : IConsumer<PumpStatusChangedEvent>
{
    private readonly IHubContext<PosHub>            _hub;
    private readonly ILogger<PumpStatusChangedConsumer> _log;

    public PumpStatusChangedConsumer(IHubContext<PosHub> hub, ILogger<PumpStatusChangedConsumer> log)
    {
        _hub = hub; _log = log;
    }

    public async Task Consume(ConsumeContext<PumpStatusChangedEvent> ctx)
    {
        var ev = ctx.Message;
        _log.LogInformation("[Consumer:PumpStatusChanged] Pump {P}: {S} | {L}L | £{A:F2}",
            ev.PumpId, ev.Status, ev.LitresDispensed, ev.Amount);

        await _hub.Clients.All.SendAsync("PumpStatusChanged", new
        {
            pumpId          = ev.PumpId,
            status          = ev.Status,
            fuelType        = ev.FuelType,
            litresDispensed = ev.LitresDispensed,
            amount          = ev.Amount,
            changedAt       = ev.ChangedAt
        });
    }
}
