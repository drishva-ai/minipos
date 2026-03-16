using Microsoft.AspNetCore.Mvc;
using POS.WebHost.Services;

namespace POS.WebHost.Controllers;

/// <summary>Authentication endpoints — issue and validate JWT tokens.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly ITokenService _tokens;
    private readonly ILogger<AuthController> _log;

    public AuthController(ITokenService tokens, ILogger<AuthController> log)
    {
        _tokens = tokens; _log = log;
    }

    /// <summary>
    /// Sign in — returns a JWT token for use with SignalR and Swagger.
    /// Demo credentials: cashier@pos / password123
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        // Production: replace with Identity + password hash check
        var users = new Dictionary<string, (string Hash, string Role)>
        {
            { "cashier@pos",  ("password123", "Cashier")  },
            { "cashier2@pos", ("password123", "Cashier")  },
            { "manager@pos",  ("manager123",  "Manager")  }
        };

        if (!users.TryGetValue(req.Username.ToLowerInvariant(), out var entry)
            || entry.Hash != req.Password)
        {
            _log.LogWarning("[Auth] Failed login attempt for {User}", req.Username);
            return Unauthorized(new ProblemDetails
            {
                Title  = "Authentication failed",
                Detail = "Invalid username or password"
            });
        }

        var cashierId = req.Username.Split('@')[0];
        var token     = _tokens.GenerateToken(cashierId, req.Username, entry.Role);

        _log.LogInformation("[Auth] Login OK — Cashier: {C} | Role: {R}", cashierId, entry.Role);

        return Ok(new LoginResponse
        {
            Token      = token,
            CashierId  = cashierId,
            Username   = req.Username,
            Role       = entry.Role,
            ExpiresAt  = DateTime.UtcNow.AddHours(8)
        });
    }

    /// <summary>Verify a JWT token is valid (used by the UI on page reload).</summary>
    [HttpGet("verify")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ProducesResponseType(typeof(VerifyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Verify() =>
        Ok(new VerifyResponse
        {
            CashierId = User.Identity?.Name ?? "unknown",
            Valid     = true
        });
}

// ── Request / Response DTOs ───────────────────────────────────────────────────

public sealed record LoginRequest
{
    public required string Username { get; init; }
    public required string Password { get; init; }
}

public sealed record LoginResponse
{
    public required string   Token     { get; init; }
    public required string   CashierId { get; init; }
    public required string   Username  { get; init; }
    public required string   Role      { get; init; }
    public required DateTime ExpiresAt { get; init; }
}

public sealed record VerifyResponse
{
    public required string CashierId { get; init; }
    public required bool   Valid     { get; init; }
}
