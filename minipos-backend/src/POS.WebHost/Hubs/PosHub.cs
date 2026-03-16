using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MiniPOS.Shared.Messages;

namespace POS.WebHost.Hubs;

/// <summary>
/// The SignalR Hub — the real-time bridge between React and the microservices.
///
/// ALL cashier actions that need to mutate state go through here.
/// This hub does NOT do business logic — it only validates the JWT
/// and publishes a MassTransit command to the appropriate queue.
///
/// Pattern:  React.invoke() → Hub method → _bus.Publish() → RabbitMQ → Service
///           React.on()     ← SignalR push ← POS.WebHost consumer ← RabbitMQ ← Service event
/// </summary>
[Authorize]
public sealed class PosHub : Hub
{
    private readonly IPublishEndpoint  _bus;
    private readonly ILogger<PosHub>   _log;

    public PosHub(IPublishEndpoint bus, ILogger<PosHub> log)
    {
        _bus = bus;
        _log = log;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override async Task OnConnectedAsync()
    {
        var cashier = CashierId();
        _log.LogInformation("[PosHub] Connected — Cashier: {Cashier} | ConnId: {ConnId}",
            cashier, Context.ConnectionId);

        await Clients.Caller.SendAsync("Connected", new
        {
            cashierId = cashier,
            message   = "SignalR connection established",
            timestamp = DateTime.UtcNow
        });

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _log.LogInformation("[PosHub] Disconnected — ConnId: {ConnId} | Reason: {Reason}",
            Context.ConnectionId, exception?.Message ?? "clean");

        await base.OnDisconnectedAsync(exception);
    }

    // ── Basket Methods ────────────────────────────────────────────────────────

    /// <summary>
    /// React: await connection.invoke("AddItem", basketId, barcode)
    /// Publishes AddItemCommand → basket-queue → Basket.Service
    /// Basket.Service calls Articles.Service, updates Redis, publishes BasketUpdatedEvent
    /// BasketUpdatedConsumer here receives it and calls Clients.All.SendAsync("BasketUpdated")
    /// </summary>
    public async Task AddItem(string basketId, string barcode)
    {
        _log.LogInformation("[Hub:AddItem] Basket: {B} | Barcode: {Bc} | Cashier: {C}",
            basketId, barcode, CashierId());

        await _bus.Publish(new AddItemCommand
        {
            BasketId  = basketId,
            Barcode   = barcode,
            CashierId = CashierId()
        });
    }

    /// <summary>React: await connection.invoke("RemoveItem", basketId, itemId)</summary>
    public async Task RemoveItem(string basketId, int itemId)
    {
        _log.LogInformation("[Hub:RemoveItem] Basket: {B} | Item: {I}", basketId, itemId);
        await _bus.Publish(new RemoveItemCommand { BasketId = basketId, ItemId = itemId });
    }

    /// <summary>React: await connection.invoke("AbortBasket", basketId)</summary>
    public async Task AbortBasket(string basketId)
    {
        _log.LogWarning("[Hub:AbortBasket] Basket: {B} | Cashier: {C}", basketId, CashierId());
        await _bus.Publish(new AbortBasketCommand { BasketId = basketId, CashierId = CashierId() });
    }

    // ── Payment Methods ───────────────────────────────────────────────────────

    /// <summary>
    /// React: await connection.invoke("ProcessPayment", basketId, method, total, items)
    /// Publishes ProcessPaymentCommand → payment-queue → Payment.Service
    /// </summary>
    public async Task ProcessPayment(
        string basketId, string paymentMethod, decimal total,
        List<MiniPOS.Shared.Messages.BasketItemDto> items)
    {
        _log.LogInformation("[Hub:ProcessPayment] Basket: {B} | Method: {M} | Total: £{T:F2}",
            basketId, paymentMethod, total);

        await _bus.Publish(new ProcessPaymentCommand
        {
            BasketId      = basketId,
            CashierId     = CashierId(),
            PaymentMethod = paymentMethod,
            Total         = total,
            Items         = items
        });
    }

    // ── Forecourt Methods ─────────────────────────────────────────────────────

    /// <summary>React: await connection.invoke("AuthorisePump", pumpId)</summary>
    public async Task AuthorisePump(int pumpId)
    {
        _log.LogInformation("[Hub:AuthorisePump] Pump: {P} | Cashier: {C}", pumpId, CashierId());
        await _bus.Publish(new AuthorisePumpCommand { PumpId = pumpId, CashierId = CashierId() });
    }

    /// <summary>React: await connection.invoke("StopPump", pumpId)</summary>
    public async Task StopPump(int pumpId)
    {
        _log.LogInformation("[Hub:StopPump] Pump: {P}", pumpId);
        await _bus.Publish(new StopPumpCommand { PumpId = pumpId, CashierId = CashierId() });
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private string CashierId() =>
        Context.User?.Identity?.Name ?? Context.ConnectionId[..8];
}
