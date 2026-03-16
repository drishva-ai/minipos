using Microsoft.AspNetCore.Mvc;
using Payment.Service.Services;

namespace Payment.Service.Controllers;

[ApiController]
[Route("api/payment")]
[Produces("application/json")]
public sealed class PaymentController : ControllerBase
{
    private readonly ITransactionRepository _repo;

    public PaymentController(ITransactionRepository repo) => _repo = repo;

    /// <summary>Get all transactions — audit trail / reporting</summary>
    [HttpGet("transactions")]
    [ProducesResponseType(typeof(IReadOnlyList<TransactionRecord>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() => Ok(await _repo.GetAllAsync());

    /// <summary>Get single transaction by ID</summary>
    [HttpGet("transactions/{id}")]
    [ProducesResponseType(typeof(TransactionRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        var t = await _repo.GetByIdAsync(id);
        return t is null ? NotFound() : Ok(t);
    }
}
