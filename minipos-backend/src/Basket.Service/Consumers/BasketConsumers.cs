using Basket.Service.Services;
using MassTransit;
using MiniPOS.Shared.Messages;

namespace Basket.Service.Consumers;

/// <summary>
/// Consumes AddItemCommand from basket-queue.
///
/// Full flow:
///   1. Get existing basket from Redis (or create a new empty one)
///   2. Call Articles.Service REST GET /api/v1/articles/barcode/{barcode}
///   3. If found: add or increment quantity in basket
///   4. Save updated basket back to Redis
///   5. Publish BasketUpdatedEvent → pos-webhost-events → POS.WebHost → SignalR → React
/// </summary>
public sealed class AddItemConsumer : IConsumer<AddItemCommand>
{
    private readonly IBasketRepository _repo;
    private readonly IArticlesClient   _articles;
    private readonly IPublishEndpoint  _bus;
    private readonly ILogger<AddItemConsumer> _log;

    public AddItemConsumer(IBasketRepository repo, IArticlesClient articles,
        IPublishEndpoint bus, ILogger<AddItemConsumer> log)
    {
        _repo = repo; _articles = articles; _bus = bus; _log = log;
    }

    public async Task Consume(ConsumeContext<AddItemCommand> ctx)
    {
        var cmd = ctx.Message;
        _log.LogInformation("[AddItem] BasketId: {B} | Barcode: {Bc} | Cashier: {C}",
            cmd.BasketId, cmd.Barcode, cmd.CashierId);

        // 1. Load or create basket
        var basket = await _repo.GetAsync(cmd.BasketId, ctx.CancellationToken)
            ?? new BasketState { BasketId = cmd.BasketId, CashierId = cmd.CashierId };

        // 2. Resolve article from Articles.Service
        var article = await _articles.GetByBarcodeAsync(cmd.Barcode, ctx.CancellationToken);
        if (article is null)
        {
            _log.LogWarning("[AddItem] Unknown barcode: {Bc}", cmd.Barcode);
            return;
        }

        // 3. Add or increment
        var existingIndex = basket.Items.FindIndex(i => i.Barcode == cmd.Barcode);
        if (existingIndex >= 0)
        {
            var existing = basket.Items[existingIndex];
            basket.Items[existingIndex] = existing with { Quantity = existing.Quantity + 1 };
        }
        else
        {
            basket.Items.Add(new BasketItemDto
            {
                Id       = article.Id,
                Name     = article.Name,
                Barcode  = article.Barcode,
                Price    = article.Price,
                Quantity = 1,
                IsFuel   = article.IsFuel,
                Emoji    = article.Emoji,
                Category = article.Category
            });
        }

        // 4. Persist to Redis
        await _repo.SaveAsync(basket, ctx.CancellationToken);

        // 5. Notify POS.WebHost → React
        await _bus.Publish(new BasketUpdatedEvent
        {
            BasketId  = basket.BasketId,
            CashierId = basket.CashierId,
            Items     = basket.Items,
            Total     = basket.Total
        });

        _log.LogInformation("[AddItem] Done — {N} items, Total: £{T:F2}",
            basket.Items.Count, basket.Total);
    }
}

/// <summary>Removes a line item from the basket by ID.</summary>
public sealed class RemoveItemConsumer : IConsumer<RemoveItemCommand>
{
    private readonly IBasketRepository _repo;
    private readonly IPublishEndpoint  _bus;
    private readonly ILogger<RemoveItemConsumer> _log;

    public RemoveItemConsumer(IBasketRepository repo, IPublishEndpoint bus,
        ILogger<RemoveItemConsumer> log)
    {
        _repo = repo; _bus = bus; _log = log;
    }

    public async Task Consume(ConsumeContext<RemoveItemCommand> ctx)
    {
        var cmd    = ctx.Message;
        var basket = await _repo.GetAsync(cmd.BasketId, ctx.CancellationToken);
        if (basket is null) return;

        basket.Items.RemoveAll(i => i.Id == cmd.ItemId);
        await _repo.SaveAsync(basket, ctx.CancellationToken);

        await _bus.Publish(new BasketUpdatedEvent
        {
            BasketId  = basket.BasketId,
            CashierId = basket.CashierId,
            Items     = basket.Items,
            Total     = basket.Total
        });

        _log.LogInformation("[RemoveItem] ItemId {I} removed from {B}", cmd.ItemId, cmd.BasketId);
    }
}

/// <summary>Deletes the basket entirely and resets the UI.</summary>
public sealed class AbortBasketConsumer : IConsumer<AbortBasketCommand>
{
    private readonly IBasketRepository _repo;
    private readonly IPublishEndpoint  _bus;
    private readonly ILogger<AbortBasketConsumer> _log;

    public AbortBasketConsumer(IBasketRepository repo, IPublishEndpoint bus,
        ILogger<AbortBasketConsumer> log)
    {
        _repo = repo; _bus = bus; _log = log;
    }

    public async Task Consume(ConsumeContext<AbortBasketCommand> ctx)
    {
        var cmd = ctx.Message;
        await _repo.DeleteAsync(cmd.BasketId, ctx.CancellationToken);

        // Publish empty basket — React resets to clean slate
        await _bus.Publish(new BasketUpdatedEvent
        {
            BasketId  = cmd.BasketId,
            CashierId = cmd.CashierId,
            Items     = [],
            Total     = 0
        });

        _log.LogWarning("[AbortBasket] {B} aborted. Reason: {R}", cmd.BasketId, cmd.Reason);
    }
}
