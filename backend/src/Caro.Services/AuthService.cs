using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Caro.Core.Entities;
using Caro.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Caro.Services;

public class AuthService : IAuthService
{
    private readonly CaroDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(CaroDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<User> RegisterAsync(string username, string password, string? displayName, CancellationToken ct = default)
    {
        if (await _db.Users.AnyAsync(u => u.Username == username, ct))
            throw new InvalidOperationException("Username already exists");

        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    public async Task<(User user, string token)> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username, ct)
                   ?? throw new InvalidOperationException("Invalid credentials");
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new InvalidOperationException("Invalid credentials");
        var token = GenerateJwtToken(user);
        return (user, token);
    }

    public string GenerateJwtToken(User user)
    {
        var key = _config["Jwt:Key"] ?? "super_secret_dev_key_change_me_to_production_256bits_minimum_required_here";
        var issuer = _config["Jwt:Issuer"] ?? "Caro";
        var audience = _config["Jwt:Audience"] ?? "CaroClients";

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("username", user.Username)
        };

        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims, expires: DateTime.UtcNow.AddDays(7), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}


