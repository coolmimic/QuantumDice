using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuantumDice.Api.DTOs;
using QuantumDice.Infrastructure.Data;

namespace QuantumDice.Api.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAdminAsync(LoginRequest request);
    Task<LoginResponse?> LoginDealerAsync(LoginRequest request);
    ClaimsPrincipal? ValidateToken(string token);
}

public class AuthService : IAuthService
{
    private readonly QuantumDiceDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(QuantumDiceDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<LoginResponse?> LoginAdminAsync(LoginRequest request)
    {
        var admin = await _db.SuperAdmins
            .FirstOrDefaultAsync(a => a.Username == request.Username && a.IsActive);

        if (admin == null || !BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash))
            return null;

        var token = GenerateToken(admin.Id.ToString(), admin.Username, "Admin");
        var expiresAt = DateTime.UtcNow.AddHours(24);

        return new LoginResponse(token, admin.Username, "Admin", expiresAt);
    }

    public async Task<LoginResponse?> LoginDealerAsync(LoginRequest request)
    {
        var dealer = await _db.Dealers
            .FirstOrDefaultAsync(d => d.Username == request.Username && d.IsActive);

        if (dealer == null || !BCrypt.Net.BCrypt.Verify(request.Password, dealer.PasswordHash))
            return null;

        // 检查订阅是否有效
        var hasValidSubscription = await _db.Subscriptions
            .AnyAsync(s => s.DealerId == dealer.Id 
                && s.Status == Core.Enums.SubscriptionStatus.Active 
                && s.EndTime > DateTime.UtcNow);

        if (!hasValidSubscription)
            return null;

        var token = GenerateToken(dealer.Id.ToString(), dealer.Username, "Dealer");
        var expiresAt = DateTime.UtcNow.AddHours(24);

        return new LoginResponse(token, dealer.Username, "Dealer", expiresAt);
    }

    private string GenerateToken(string userId, string username, string role)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "QuantumDice_SuperSecretKey_2024!@#$%^"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "QuantumDice",
            audience: _config["Jwt:Audience"] ?? "QuantumDice",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "QuantumDice_SuperSecretKey_2024!@#$%^"));

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _config["Jwt:Issuer"] ?? "QuantumDice",
                ValidAudience = _config["Jwt:Audience"] ?? "QuantumDice",
                IssuerSigningKey = key
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
