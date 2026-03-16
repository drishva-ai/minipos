using Forecourt.Service.Services;
using Microsoft.AspNetCore.Mvc;

namespace Forecourt.Service.Controllers;

[ApiController]
[Route("api/forecourt")]
[Produces("application/json")]
public sealed class ForecourtController : ControllerBase
{
    private readonly IPumpStateService _pumps;

    public ForecourtController(IPumpStateService pumps) => _pumps = pumps;

    /// <summary>
    /// Get all pump states.
    /// Called by React on page load to populate the Forecourt tab.
    /// Live updates then come via SignalR PumpStatusChanged (no polling needed).
    /// </summary>
    [HttpGet("pumps")]
    [ProducesResponseType(typeof(IReadOnlyList<PumpState>), StatusCodes.Status200OK)]
    public IActionResult GetPumps() => Ok(_pumps.GetAll());

    /// <summary>Get a single pump by ID</summary>
    [HttpGet("pumps/{id:int}")]
    [ProducesResponseType(typeof(PumpState), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetPump(int id)
    {
        var pump = _pumps.Get(id);
        return pump is null ? NotFound() : Ok(pump);
    }
}
