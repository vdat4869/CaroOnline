using Caro.Services;
using Caro.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Caro.Api.Services;

namespace Caro.Api.Hubs;

[Authorize]
public class GameHub : Hub
{
    private readonly IGameService _gameService;
    private readonly IAiService _aiService;
    private readonly PresenceService _presence;
    private readonly ChallengeService _challenges;
    private readonly GameTimerService _timers;

    public GameHub(IGameService gameService, IAiService aiService, PresenceService presence, ChallengeService challenges, GameTimerService timers)
    {
        _gameService = gameService;
        _aiService = aiService;
        _presence = presence;
        _challenges = challenges;
        _timers = timers;
    }

    private int GetUserId() => int.TryParse(Context.User?.FindFirstValue("sub"), out var id) ? id : 0;

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId > 0)
        {
            _presence.OnConnected(userId, Context.ConnectionId);
            await Clients.Group("lobby").SendAsync("LobbyUpdate", new { usersOnline = _presence.GetOnlineUserIds().Length });
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _presence.OnDisconnected(Context.ConnectionId);
        await Clients.Group("lobby").SendAsync("LobbyUpdate", new { usersOnline = _presence.GetOnlineUserIds().Length });
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinLobby()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "lobby");
        await Clients.Caller.SendAsync("LobbyUpdate", new { usersOnline = _presence.GetOnlineUserIds().Length });
    }

    public async Task JoinGame(int gameId)
    {
        var group = $"game-{gameId}";
        // Ensure this user is registered as a participant of the game (assigns P2 if free)
        var userId = GetUserId();
        try
        {
            await _gameService.EnsurePlayerAsync(gameId, userId);
        }
        catch (Exception ex)
        {
            throw new HubException(ex.Message);
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        await Clients.Group(group).SendAsync("PlayerJoined", new { gameId, userId });
    }

    public async Task SendChallenge(int targetUserId)
    {
        var fromId = GetUserId();
        var challenge = _challenges.Create(fromId, targetUserId);
        var connections = _presence.GetConnectionsForUser(targetUserId);
        foreach (var cid in connections)
        {
            await Clients.Client(cid).SendAsync("ChallengeReceived", new { id = challenge.Id, fromUserId = fromId });
        }
        await Clients.Caller.SendAsync("ChallengeSent", new { id = challenge.Id, toUserId = targetUserId });
    }

    public async Task AcceptChallenge(string challengeId)
    {
        if (!Guid.TryParse(challengeId, out var id)) throw new HubException("Invalid challengeId");
        var challenge = _challenges.Get(id) ?? throw new HubException("Challenge not found");
        var me = GetUserId();
        if (challenge.ToUserId != me) throw new HubException("Not target user");

        _challenges.Remove(id);
        var game = await _gameService.CreateGameAsync(new CreateGameOptions("PvP", challenge.FromUserId, challenge.ToUserId, null, 60));

        // Add both to game group
        var group = $"game-{game.Id}";
        foreach (var uid in new[] { challenge.FromUserId, challenge.ToUserId })
        {
            foreach (var cid in _presence.GetConnectionsForUser(uid))
            {
                await Groups.AddToGroupAsync(cid, group);
            }
        }

        // Start timer
        _timers.Start(game.Id, game.TimeControlSeconds, game.CurrentTurn);

        await Clients.Group(group).SendAsync("GameStarted", new { gameId = game.Id, mode = game.Mode });
        await Clients.Clients(_presence.GetConnectionsForUser(challenge.FromUserId)).SendAsync("ChallengeAccepted", challenge.Id, new { gameId = game.Id });
    }

    public async Task<object> CreateGame(string mode, int? p1UserId, int? p2UserId, int? pveDifficulty, int timeControlSeconds)
    {
        var game = await _gameService.CreateGameAsync(new CreateGameOptions(mode, p1UserId, p2UserId, pveDifficulty, timeControlSeconds));
        var group = $"game-{game.Id}";
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        _timers.Start(game.Id, game.TimeControlSeconds, game.CurrentTurn);
        await Clients.Caller.SendAsync("GameStarted", new { gameId = game.Id, mode = game.Mode });
        return new { game.Id };
    }

    public async Task MakeMove(int gameId, int x, int y)
    {
        try
        {
            var userId = GetUserId();
            var player = await _gameService.EnsurePlayerAsync(gameId, userId);
            var game = await _gameService.GetGameAsync(gameId) ?? throw new HubException("Game not found");
            var move = await _gameService.MakeMoveAsync(gameId, player, x, y);
            
            // Refresh game to check if it's finished (win detected)
            var updatedGame = await _gameService.GetGameAsync(gameId);
            bool isWin = updatedGame?.FinishedAt != null;
            
            await Clients.Group($"game-{gameId}").SendAsync("MoveMade", new { gameId, player = move.Player, x = move.X, y = move.Y, moveNumber = move.MoveNumber });
            
            if (isWin)
            {
                var winner = updatedGame!.Result!.StartsWith("P1") ? 1 : 2;
                await Clients.Group($"game-{gameId}").SendAsync("GameEnded", new { gameId, result = updatedGame.Result, winner });
                _timers.Stop(gameId);
                return;
            }
            
            _timers.SwitchTurn(gameId, move.Player == 1 ? 2 : 1);
            
            // Nếu là PvE mode và vừa player 1 move xong, tự động make move cho AI (player 2)
            if (game.Mode == "PvE" && game.PveDifficulty.HasValue && move.Player == 1)
            {
                // Đợi ngắn để client update UI (giảm xuống 100ms)
                await Task.Delay(100);
                
                // Lấy game với moves mới nhất để build board state
                var gameWithMoves = await GetGameWithMovesForAi(gameId);
                if (gameWithMoves == null || gameWithMoves.FinishedAt != null) return;
                
                // Build board state từ moves
                var board = BuildBoardFromMoves(gameWithMoves.Moves);
                
                // AI là player 2, difficulty từ game
                var aiMove = _aiService.ChooseMove(board, 2, (AiDifficulty)game.PveDifficulty.Value);
                
                // Make AI move
                var aiMoveResult = await _gameService.MakeMoveAsync(gameId, 2, aiMove.X, aiMove.Y);
                
                // Check if AI won
                var aiGameAfterMove = await _gameService.GetGameAsync(gameId);
                bool aiWin = aiGameAfterMove?.FinishedAt != null;
                
                await Clients.Group($"game-{gameId}").SendAsync("MoveMade", new { gameId, player = aiMoveResult.Player, x = aiMoveResult.X, y = aiMoveResult.Y, moveNumber = aiMoveResult.MoveNumber });
                
                if (aiWin)
                {
                    var winner = aiGameAfterMove!.Result!.StartsWith("P1") ? 1 : 2;
                    await Clients.Group($"game-{gameId}").SendAsync("GameEnded", new { gameId, result = aiGameAfterMove.Result, winner });
                    _timers.Stop(gameId);
                    return;
                }
                
                _timers.SwitchTurn(gameId, 1);
            }
        }
        catch (DbUpdateException dbex)
        {
            var msg = dbex.InnerException?.Message ?? dbex.Message;
            throw new HubException($"DB error: {msg}");
        }
        catch (InvalidOperationException ex)
        {
            // Wrap InvalidOperationException thành HubException để SignalR trả về đúng cho client
            throw new HubException(ex.Message);
        }
        catch (Exception ex)
        {
            // Log other exceptions and re-throw as HubException
            throw new HubException($"Error making move: {ex.Message}");
        }
    }
    
    private async Task<Game?> GetGameWithMovesForAi(int gameId)
    {
        var game = await _gameService.GetGameAsync(gameId);
        if (game == null) return null;
        
        // Get moves và set vào game object để AI có thể tính toán
        var moves = await _gameService.GetMovesAsync(gameId);
        return new Game
        {
            Id = game.Id,
            Mode = game.Mode,
            PveDifficulty = game.PveDifficulty,
            FinishedAt = game.FinishedAt,
            Moves = moves
        };
    }
    
    private static int[,] BuildBoardFromMoves(List<Move> moves)
    {
        const int size = 15;
        var board = new int[size, size];
        foreach (var move in moves)
        {
            board[move.X, move.Y] = move.Player;
        }
        return board;
    }

    public async Task Resign(int gameId)
    {
        var game = await _gameService.GetGameAsync(gameId) ?? throw new HubException("Game not found");
        await _gameService.ResignAsync(gameId, game.CurrentTurn);
        await Clients.Group($"game-{gameId}").SendAsync("GameEnded", new { gameId, result = "resign" });
        _timers.Stop(gameId);
    }

    public Task Ping()
    {
        return Clients.Caller.SendAsync("Pong");
    }
}


