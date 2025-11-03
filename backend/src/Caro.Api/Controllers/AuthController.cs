using Caro.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    public record RegisterRequest(string Username, string Password, string? DisplayName);
    public record LoginRequest(string Username, string Password);

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        var user = await _authService.RegisterAsync(req.Username, req.Password, req.DisplayName, ct);
        var token = _authService.GenerateJwtToken(user);
        return Ok(new { user.Id, user.Username, user.DisplayName, token });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var (user, token) = await _authService.LoginAsync(req.Username, req.Password, ct);
        return Ok(new { user.Id, user.Username, user.DisplayName, token });
    }
}


