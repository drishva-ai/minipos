using Microsoft.AspNetCore.Mvc;

namespace Articles.Service.Controllers;

/// <summary>
/// Product catalogue — serves articles to the React UI and to Basket.Service.
///
/// Called by:
///   - React UI on load: GET /api/v1/articles → product grid
///   - Basket.Service on scan: GET /api/v1/articles/barcode/{barcode} → item details
/// </summary>
[ApiController]
[Route("api/v1/articles")]
[Produces("application/json")]
public sealed class ArticlesController : ControllerBase
{
    // In production this would be an EF Core DbContext + SQL Server.
    // Static list here to keep the demo self-contained.
    private static readonly IReadOnlyList<ArticleDto> Catalogue = new List<ArticleDto>
    {
        // ── Drinks ────────────────────────────────────────────────────────────
        new(1,  "Cola 500ml",          "5000112637922",  1.50m, "🥤", "Drinks"),
        new(2,  "Mineral Water 750ml", "5000167024013",  1.00m, "💧", "Drinks"),
        new(3,  "Orange Juice 1L",     "5010069150600",  2.00m, "🍊", "Drinks"),
        new(4,  "Coffee (Cup)",        "4006381333931",  2.50m, "☕", "Drinks"),
        new(5,  "Energy Drink 250ml",  "5060058540016",  2.20m, "⚡", "Drinks"),
        // ── Food ──────────────────────────────────────────────────────────────
        new(6,  "Meal Deal Sandwich",  "5000436336468",  3.50m, "🥪", "Food"),
        new(7,  "Hot Dog",             "4008400402918",  2.80m, "🌭", "Food"),
        new(8,  "Croissant",           "5001123456789",  1.80m, "🥐", "Food"),
        new(9,  "Sausage Roll",        "5002234567890",  1.60m, "🌯", "Food"),
        // ── Snacks ────────────────────────────────────────────────────────────
        new(10, "Crisps",              "5000328201008",  1.20m, "🍟", "Snacks"),
        new(11, "Chocolate Bar",       "7622210951779",  1.10m, "🍫", "Snacks"),
        new(12, "Chewing Gum",         "7613034626844",  0.80m, "🟢", "Snacks"),
        new(13, "Nuts Mix",            "5003345678901",  1.80m, "🥜", "Snacks"),
        // ── Motoring ──────────────────────────────────────────────────────────
        new(14, "Screen Wash 5L",      "5004456789012",  4.99m, "🧴", "Motoring"),
        new(15, "Air Freshener",       "5005567890123",  2.50m, "🌸", "Motoring"),
        new(16, "Phone Charger 1m",    "0885909950805",  8.99m, "🔌", "Motoring"),
        // ── Other ─────────────────────────────────────────────────────────────
        new(17, "Newspaper",           "9770261239003",  1.50m, "📰", "Other"),
        new(18, "Paracetamol 16s",     "5000158124820",  3.50m, "💊", "Other"),
        new(19, "Chewing Tobacco",     "5006678901234",  6.99m, "🚫", "Other"),
        // ── Fuel barcodes (virtual — created by Forecourt.Service) ────────────
        new(1001, "Diesel — Pump 1",   "FUEL-01", 0.00m, "⛽", "Fuel",  true),
        new(1002, "Unleaded — Pump 2", "FUEL-02", 0.00m, "⛽", "Fuel",  true),
        new(1003, "Premium — Pump 3",  "FUEL-03", 0.00m, "⛽", "Fuel",  true),
        new(1004, "Diesel — Pump 4",   "FUEL-04", 0.00m, "⛽", "Fuel",  true),
        new(1005, "Unleaded — Pump 5", "FUEL-05", 0.00m, "⛽", "Fuel",  true),
        new(1006, "Premium — Pump 6",  "FUEL-06", 0.00m, "⛽", "Fuel",  true),
    };

    /// <summary>
    /// Get all shop articles. Pass ?category=Drinks to filter.
    /// Called by React to populate the product grid.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ArticleDto>), StatusCodes.Status200OK)]
    public IActionResult GetAll([FromQuery] string? category = null)
    {
        // Exclude fuel from the shop grid — fuel is shown in Forecourt tab
        var items = Catalogue.Where(a => !a.IsFuel);
        if (!string.IsNullOrWhiteSpace(category))
            items = items.Where(a => a.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        return Ok(items);
    }

    /// <summary>
    /// Get article by barcode. Called by Basket.Service (internal).
    /// Example: GET /api/v1/articles/barcode/5000112637922
    /// Returns Cola 500ml @ £1.50
    /// </summary>
    [HttpGet("barcode/{barcode}")]
    [ProducesResponseType(typeof(ArticleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetByBarcode(string barcode)
    {
        var article = Catalogue.FirstOrDefault(a =>
            a.Barcode.Equals(barcode, StringComparison.OrdinalIgnoreCase));

        return article is null
            ? NotFound(new ProblemDetails { Title = $"Barcode '{barcode}' not found" })
            : Ok(article);
    }

    /// <summary>Get article by numeric ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ArticleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(int id)
    {
        var article = Catalogue.FirstOrDefault(a => a.Id == id);
        return article is null ? NotFound() : Ok(article);
    }

    /// <summary>Get unique category list for the React category filter tabs.</summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public IActionResult GetCategories() =>
        Ok(Catalogue
            .Where(a => !a.IsFuel)
            .Select(a => a.Category)
            .Distinct()
            .OrderBy(c => c));
}

/// <summary>Article DTO — returned to React UI and to Basket.Service.</summary>
public sealed record ArticleDto(
    int     Id,
    string  Name,
    string  Barcode,
    decimal Price,
    string  Emoji,
    string  Category,
    bool    IsFuel = false);
