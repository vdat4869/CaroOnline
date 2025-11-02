# Caro (Gomoku) Online

Backend: ASP.NET Core 8 (Web API), SignalR, EF Core, JWT

Frontend: React + Vite + TailwindCSS + SignalR client

Database: SQL Server (dev uses `sa`)

## Structure

```
/caro
  /backend
    /src
      /Caro.Api
      /Caro.Core
      /Caro.Infrastructure
      /Caro.Services
  /frontend
  /sql
    init_schema.sql
  docker-compose.yml
```

## Run (Docker)

1. Update `docker-compose.yml` SA password if needed.
2. Start services:

```bash
docker compose up -d --build
```

- API: http://localhost:8080 (Swagger enabled in Development)
- Frontend: http://localhost:5173
- SignalR Hub: http://localhost:8080/hub/game

## Local Development

Backend (dotnet):

```bash
cd backend/src/Caro.Api
# dotnet restore
# dotnet run
```

Frontend (node 20):

```bash
cd frontend
npm install
npm run dev
```

## API Endpoints (initial)

- POST `/api/auth/register`
- POST `/api/auth/login`
- GET `/api/users/me` (Auth)
- GET `/api/games`
- GET `/api/games/{id}`
- GET `/api/games/{id}/moves`

## SignalR Hub Methods (initial)

Client → Server
- JoinLobby()
- SendChallenge(targetUserId)
- CreateGame(mode, p1UserId, p2UserId, pveDifficulty, timeControlSeconds)
- MakeMove(gameId, x, y)
- Resign(gameId)
- Ping()

Server → Client (names to be matched in UI progressively)
- LobbyUpdate(usersOnline)
- GameStarted(gameInfo)
- MoveMade(moveInfo)
- GameEnded(result)

## Notes

- Timer and AI are stubbed for now; to be expanded with server-side management and minimax/alpha-beta.
- Connection string and JWT key are configured in `appsettings.Development.json` and docker env.
- DB schema is bootstraped by `sql/init_schema.sql` for local dev.
