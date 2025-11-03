using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Caro.Api.Hubs;

namespace Caro.Api.Services;

public class GameTimerService
{
    private class TimerState
    {
        public int GameId { get; init; }
        public int RemainingP1 { get; set; }
        public int RemainingP2 { get; set; }
        public int ActivePlayer { get; set; }
        public CancellationTokenSource Cts { get; } = new();
    }

    private readonly IHubContext<GameHub> _hub;
    private readonly ConcurrentDictionary<int, TimerState> _timers = new();

    public GameTimerService(IHubContext<GameHub> hub)
    {
        _hub = hub;
    }

    public void Start(int gameId, int timeControlSeconds, int startingPlayer)
    {
        var state = new TimerState
        {
            GameId = gameId,
            RemainingP1 = timeControlSeconds,
            RemainingP2 = timeControlSeconds,
            ActivePlayer = startingPlayer
        };
        if (!_timers.TryAdd(gameId, state)) return;
        _ = RunTimer(state);
    }

    public void Stop(int gameId)
    {
        if (_timers.TryRemove(gameId, out var state))
        {
            state.Cts.Cancel();
        }
    }

    public void SwitchTurn(int gameId, int nextPlayer)
    {
        if (_timers.TryGetValue(gameId, out var state))
        {
            state.ActivePlayer = nextPlayer;
        }
    }

    private async Task RunTimer(TimerState state)
    {
        try
        {
            while (!state.Cts.IsCancellationRequested)
            {
                await Task.Delay(1000, state.Cts.Token);
                if (state.ActivePlayer == 1)
                {
                    state.RemainingP1 = Math.Max(0, state.RemainingP1 - 1);
                }
                else
                {
                    state.RemainingP2 = Math.Max(0, state.RemainingP2 - 1);
                }
                await _hub.Clients.Group($"game-{state.GameId}").SendAsync("UpdateTimer", state.ActivePlayer, state.ActivePlayer == 1 ? state.RemainingP1 : state.RemainingP2);

                if (state.RemainingP1 == 0 || state.RemainingP2 == 0)
                {
                    var loser = state.RemainingP1 == 0 ? 1 : 2;
                    var result = loser == 1 ? "P2_WIN_TIME" : "P1_WIN_TIME";
                    // Mark finished
                    await _hub.Clients.Group($"game-{state.GameId}").SendAsync("GameEnded", new { gameId = state.GameId, result });
                    Stop(state.GameId);
                    return;
                }
            }
        }
        catch (TaskCanceledException)
        {
        }
    }
}


