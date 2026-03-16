using Microsoft.Extensions.Caching.Distributed;
using MiniPOS.Shared.Messages;
using System.Text.Json;

namespace Basket.Service.Services;

// ── Domain model (private to this service) ────────────────────────────────────
public sealed class BasketState
{
    public required string           BasketId  { get; set; }
    public required string           CashierId { get; set; }
    public List<BasketItemDto>       Items     { get; set; } = [];
    public decimal                   Total     => Items.Sum(i => i.Price * i.Quantity);
    public DateTime                  CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime                  UpdatedAt { get; set; } = DateTime.UtcNow;
}

// ── Repository interface ──────────────────────────────────────────────────────
public interface IBasketRepository
{
    Task<BasketState?> GetAsync(string basketId, CancellationToken ct = default);
    Task SaveAsync(BasketState basket, CancellationToken ct = default);
    Task DeleteAsync(string basketId, CancellationToken ct = default);
}

/// <summary>
/// Stores and retrieves baskets from Redis.
/// Key format: basket:{basketId}
/// TTL: 30 minutes — idle baskets auto-expire
/// </summary>
public sealed class RedisBasketRepository : IBasketRepository
{
    private static readonly DistributedCacheEntryOptions CacheOptions =
        new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) };

    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisBasketRepository> _log;

    public RedisBasketRepository(IDistributedCache cache, ILogger<RedisBasketRepository> log)
    {
        _cache = cache; _log = log;
    }

    public async Task<BasketState?> GetAsync(string basketId, CancellationToken ct = default)
    {
        var json = await _cache.GetStringAsync($"basket:{basketId}", ct);
        _log.LogDebug("[Redis] GET basket:{BasketId} — {Result}", basketId, json is null ? "miss" : "hit");
        return json is null ? null : JsonSerializer.Deserialize<BasketState>(json);
    }

    public async Task SaveAsync(BasketState basket, CancellationToken ct = default)
    {
        basket.UpdatedAt = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(basket);
        await _cache.SetStringAsync($"basket:{basket.BasketId}", json, CacheOptions, ct);
        _log.LogDebug("[Redis] SET basket:{BasketId} — Items: {N}, Total: £{T:F2}",
            basket.BasketId, basket.Items.Count, basket.Total);
    }

    public async Task DeleteAsync(string basketId, CancellationToken ct = default)
    {
        await _cache.RemoveAsync($"basket:{basketId}", ct);
        _log.LogDebug("[Redis] DEL basket:{BasketId}", basketId);
    }
}

// ── Articles.Service HTTP client ──────────────────────────────────────────────
public interface IArticlesClient
{
    Task<ArticleDto?> GetByBarcodeAsync(string barcode, CancellationToken ct = default);
}

public sealed record ArticleDto(
    int Id, string Name, string Barcode, decimal Price,
    string Emoji, string Category, bool IsFuel = false);

/// <summary>
/// Typed HTTP client that calls Articles.Service REST API.
/// No JWT required — internal service-to-service call within the Docker network.
/// </summary>
public sealed class ArticlesClient : IArticlesClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ArticlesClient> _log;

    public ArticlesClient(HttpClient http, ILogger<ArticlesClient> log)
    {
        _http = http; _log = log;
    }

    public async Task<ArticleDto?> GetByBarcodeAsync(string barcode, CancellationToken ct = default)
    {
        try
        {
            _log.LogDebug("[Articles] GET /api/v1/articles/barcode/{Barcode}", barcode);
            var result = await _http.GetFromJsonAsync<ArticleDto>($"/api/v1/articles/barcode/{barcode}", ct);
            _log.LogDebug("[Articles] Found: {Name} @ £{Price}", result?.Name, result?.Price);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _log.LogError(ex, "[Articles] Failed to resolve barcode {Barcode}", barcode);
            return null;
        }
    }
}
