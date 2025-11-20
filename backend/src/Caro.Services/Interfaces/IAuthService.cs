using Caro.Core.Entities;

namespace Caro.Services;

public interface IAuthService
{
    Task<User> RegisterAsync(string username, string password, string? displayName, CancellationToken ct = default);
    Task<(User user, string token)> LoginAsync(string username, string password, CancellationToken ct = default);
    string GenerateJwtToken(User user);
}


