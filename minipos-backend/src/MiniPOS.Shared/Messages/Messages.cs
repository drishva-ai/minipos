// ═══════════════════════════════════════════════════════════════════════════════
//  MiniPOS.Shared — Message Contracts
//
//  These records are the ONLY way services communicate with each other.
//  No shared databases. No direct method calls. Everything via these messages
//  over RabbitMQ (MassTransit).
//
//  Flow:
//    React → SignalR → POS.WebHost publishes Commands
//    Services consume Commands, do work, publish Events
//    POS.WebHost consumes Events, pushes to React via SignalR
// ═══════════════════════════════════════════════════════════════════════════════

namespace MiniPOS.Shared.Messages;

// ─────────────────────────────────────────────────────────────────────────────
//  BASKET COMMANDS  →  basket-queue  →  Basket.Service
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Cashier scans barcode or clicks product tile.</summary>
public record AddItemCommand
{
    public required string BasketId  { get; init; }
    public required string Barcode   { get; init; }
    public required string CashierId { get; init; }
    public DateTime IssuedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Cashier removes a line from the basket.</summary>
public record RemoveItemCommand
{
    public required string BasketId { get; init; }
    public required int    ItemId   { get; init; }
}

/// <summary>Cashier cancels the whole transaction.</summary>
public record AbortBasketCommand
{
    public required string BasketId  { get; init; }
    public required string CashierId { get; init; }
    public string Reason { get; init; } = "Cashier abort";
}

// ─────────────────────────────────────────────────────────────────────────────
//  BASKET EVENTS  →  pos-webhost-events  →  POS.WebHost  →  SignalR  →  React
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Published by Basket.Service after every mutation.
/// POS.WebHost receives this and calls hub.Clients.All.SendAsync("BasketUpdated").
/// </summary>
public record BasketUpdatedEvent
{
    public required string BasketId  { get; init; }
    public required string CashierId { get; init; }
    public List<BasketItemDto> Items  { get; init; } = [];
    public decimal Total { get; init; }
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

// ─────────────────────────────────────────────────────────────────────────────
//  PAYMENT COMMANDS  →  payment-queue  →  Payment.Service
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Cashier confirms payment method and total.</summary>
public record ProcessPaymentCommand
{
    public required string BasketId      { get; init; }
    public required string CashierId     { get; init; }
    public required string PaymentMethod { get; init; }  // Cash | Card | Contactless | Voucher
    public required decimal Total        { get; init; }
    public List<BasketItemDto> Items     { get; init; } = [];
    public DateTime IssuedAt { get; init; } = DateTime.UtcNow;
}

// ─────────────────────────────────────────────────────────────────────────────
//  PAYMENT EVENTS  →  pos-webhost-events  →  POS.WebHost  →  SignalR  →  React
// ─────────────────────────────────────────────────────────────────────────────

public record PaymentCompletedEvent
{
    public required string  BasketId       { get; init; }
    public required string  TransactionId  { get; init; }
    public required decimal Total          { get; init; }
    public required string  PaymentMethod  { get; init; }
    public string  Status    { get; init; } = "Approved";
    public string? AuthCode  { get; init; }
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;
}

// ─────────────────────────────────────────────────────────────────────────────
//  FORECOURT COMMANDS  →  forecourt-queue  →  Forecourt.Service
// ─────────────────────────────────────────────────────────────────────────────

public record AuthorisePumpCommand
{
    public required int    PumpId    { get; init; }
    public required string CashierId { get; init; }
    public DateTime IssuedAt { get; init; } = DateTime.UtcNow;
}

public record StopPumpCommand
{
    public required int    PumpId    { get; init; }
    public required string CashierId { get; init; }
}

// ─────────────────────────────────────────────────────────────────────────────
//  FORECOURT EVENTS  →  pos-webhost-events  →  POS.WebHost  →  SignalR  →  React
// ─────────────────────────────────────────────────────────────────────────────

public record PumpStatusChangedEvent
{
    public required int    PumpId          { get; init; }
    public required string Status          { get; init; }  // IDLE | FUELLING | DONE
    public required string FuelType        { get; init; }
    public decimal LitresDispensed         { get; init; }
    public decimal Amount                  { get; init; }
    public DateTime ChangedAt { get; init; } = DateTime.UtcNow;
}

// ─────────────────────────────────────────────────────────────────────────────
//  SHARED DTOs  (used in multiple messages above)
// ─────────────────────────────────────────────────────────────────────────────

public record BasketItemDto
{
    public required int     Id       { get; init; }
    public required string  Name     { get; init; }
    public required string  Barcode  { get; init; }
    public required decimal Price    { get; init; }
    public required int     Quantity { get; init; }
    public bool    IsFuel  { get; init; }
    public string  Emoji   { get; init; } = "";
    public string  Category { get; init; } = "";
    public decimal LineTotal => Price * Quantity;
}
