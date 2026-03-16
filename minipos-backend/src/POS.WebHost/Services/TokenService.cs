using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace POS.WebHost.Services;

public interface ITokenService
{
    string GenerateToken(string cashierId, string username, string role = "Cashier");
}

public sealed class TokenService : ITokenService
{
    private readonly IConfiguration _cfg;

    public TokenService(IConfiguration cfg) => _cfg = cfg;

    public string GenerateToken(string cashierId, string username, string role = "Cashier")
    {
        var key   = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_cfg["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing")));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new Claim[]
        {
            new(ClaimTypes.Name,             cashierId),
            new(ClaimTypes.Email,            username),
            new(ClaimTypes.Role,             role),
            new(JwtRegisteredClaimNames.Sub, cashierId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("minipos_role",              role)
        };

        var token = new JwtSecurityToken(
            issuer:             "minipos",
            audience:           "minipos-client",
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
