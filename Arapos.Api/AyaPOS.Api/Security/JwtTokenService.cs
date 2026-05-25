using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Ayapos.Api.Security;

public sealed class JwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;

        if (_options.SigningKey.Length < 32)
            throw new InvalidOperationException(
                "Jwt:SigningKey must be at least 32 characters.");
    }

    // =========================
    // Platform Token
    // =========================
    public string CreatePlatformToken(Guid platformUserId, string platformRole)
    {
        var claims = new List<Claim>
        {
            new Claim("scope", "platform"),
            new Claim("platformUserId", platformUserId.ToString()),
            new Claim(ClaimTypes.Role, platformRole) // ✅ مهم
        };

        return CreateToken(claims);
    }

    // =========================
    // Tenant Token
    // =========================
    public string CreateTenantToken(Guid userId, Guid tenantId, string tenantRole)
    {
        var claims = new List<Claim>
        {
            new Claim("scope", "tenant"),
            new Claim("tenantId", tenantId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()), // ✅ قياسي
            new Claim(ClaimTypes.Role, tenantRole) // ✅ هذا هو الحل
        };

        return CreateToken(claims);
    }

    private string CreateToken(IEnumerable<Claim> claims)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_options.SigningKey));

        var creds = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}