using Caro.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Caro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController : ControllerBase
{
    private readonly IGameService _gameService;

    public GamesController(IGameService gameService)
    {
        _gameService = gameService;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var games = await _gameService.GetGamesAsync(ct);
        return Ok(games.Select(g => new { g.Id, g.Mode, g.Result, g.CreatedAt, g.FinishedAt }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var game = await _gameService.GetGameAsync(id, ct);
        if (game == null) return NotFound();
        return Ok(game);
    }

    [HttpGet("{id}/moves")]
    public async Task<IActionResult> Moves(int id, CancellationToken ct)
    {
        var moves = await _gameService.GetMovesAsync(id, ct);
        return Ok(moves);
    }

    public record CreateRequest(string Mode, int? P1UserId, int? P2UserId, int? PveDifficulty, int TimeControlSeconds);

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateRequest req, CancellationToken ct)
    {
        var game = await _gameService.CreateGameAsync(new CreateGameOptions(req.Mode, req.P1UserId, req.P2UserId, req.PveDifficulty, req.TimeControlSeconds), ct);
        return Ok(game);
    }
}


