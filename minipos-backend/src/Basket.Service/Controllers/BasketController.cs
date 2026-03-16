using Basket.Service.Services;
using Microsoft.AspNetCore.Mvc;

namespace Basket.Service.Controllers;

/// <summary>
/// Read-only HTTP endpoints — for debugging Redis state via Swagger.
/// All basket mutations come through RabbitMQ, not HTTP.
/// </summary>
[ApiController]
[Route("api/basket")]
[Produces("application/json")]
public sealed class BasketController : ControllerBase
{
    private readonly IBasketRepository _repo;

    public BasketController(IBasketRepository repo) => _repo = repo;

    /// <summary>Read basket from Redis by ID (debug use only)</summary>
    [HttpGet("{basketId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBasket(string basketId)
    {
        var basket = await _repo.GetAsync(basketId);
        return basket is null
            ? NotFound(new ProblemDetails { Title = $"Basket {basketId} not found in Redis" })
            : Ok(basket);
    }
}
