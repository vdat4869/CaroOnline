# Caro (Gomoku) Online

Backend: ASP.NET Core 8 (Web API), SignalR, EF Core, JWT

Frontend: Vue 3 + TypeScript + Vite + TailwindCSS + Pinia + SignalR client

Database: SQL Server

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
```

## Development Setup

### Backend (dotnet)

```bash
cd backend/src/Caro.Api
dotnet restore
dotnet run
```

- API: http://localhost:8080 (Swagger enabled in Development)
- SignalR Hub: http://localhost:8080/hub/game

### Frontend (Node 18+)

```bash
cd frontend
npm install
npm run dev
```

- Frontend: http://localhost:80 (default port)

## API Endpoints

- POST `/api/auth/register` - Đăng ký tài khoản
- POST `/api/auth/login` - Đăng nhập
- GET `/api/users/me` - Lấy thông tin user hiện tại (Auth required)
- GET `/api/games` - Lấy danh sách games
- GET `/api/games/{id}` - Lấy thông tin game
- GET `/api/games/{id}/moves` - Lấy danh sách nước đi của game

## SignalR Hub Methods

### Client → Server
- `JoinLobby()` - Tham gia lobby
- `SendChallenge(targetUserId, timeoutSeconds?)` - Gửi lời mời chơi (mặc định 10s timeout)
- `AcceptChallenge(challengeId)` - Chấp nhận lời mời
- `RejectChallenge(challengeId)` - Từ chối lời mời
- `CreateGame(mode, p1UserId, p2UserId, pveDifficulty, timeControlSeconds)` - Tạo game
- `JoinGame(gameId)` - Tham gia game
- `MakeMove(gameId, x, y)` - Đánh nước cờ
- `Resign(gameId)` - Đầu hàng
- `SendChatMessage(gameId, content)` - Gửi tin nhắn chat
- `KickPlayer(gameId, targetUserId)` - Kick player (chỉ chủ phòng)
- `Ping()` - Kiểm tra kết nối

### Server → Client
- `LobbyUpdate(usersOnline)` - Cập nhật số người online
- `ChallengeReceived(id, fromUserId, expiresAt)` - Nhận lời mời
- `ChallengeSent(id, toUserId)` - Xác nhận đã gửi lời mời
- `ChallengeCountdown(challengeId, remainingSeconds)` - Countdown lời mời
- `ChallengeAccepted(challengeId, gameId)` - Lời mời được chấp nhận
- `ChallengeRejected(challengeId, rejectedBy)` - Lời mời bị từ chối
- `ChallengeTimeout(challengeId)` - Lời mời hết hạn
- `GameStarted(gameId, mode)` - Game bắt đầu
- `PlayerJoined(gameId, userId)` - Player tham gia game
- `PlayerKicked(gameId, kickedUserId, kickedBy)` - Player bị kick
- `MoveMade(gameId, player, x, y, moveNumber)` - Nước đi được thực hiện
- `UpdateTimer(activePlayer, remainingSeconds)` - Cập nhật countdown lượt
- `GameEnded(gameId, result, winner?, ...)` - Game kết thúc
- `ChatMessage(gameId, senderId, content, timestamp)` - Tin nhắn chat
- `Pong()` - Phản hồi Ping

## Features

### Game Logic
- Board 15x15
- Check thắng: 5 quân liên tiếp, 4 quân 2 đầu trống
- Turn-based gameplay
- Countdown timer cho mỗi lượt

### AI (PvE)
- **Easy**: Random + Block 2 đầu (chặn đối thủ khi có 4 quân 2 đầu trống)
- **Medium**: Heuristic-based (đánh giá điểm số)
- **Hard**: Minimax với Alpha-Beta Pruning

### Match Management
- Tạo phòng / Join phòng
- Invite player với countdown (5-10s)
- Accept/Reject challenge
- Kick player (chỉ chủ phòng)
- Handle disconnect/timeout gracefully

### Realtime Features
- SignalR cho realtime communication
- Chat trong game
- Timer countdown realtime
- Presence tracking

### Database
- Users, Games, Moves, Messages, Rankings
- Connection string: `Server=MUMEI\SQLSERVER;Database=CaroDb;User Id=sa;Password=Hoyo@4869;TrustServerCertificate=True;`

## Notes

- Connection string và JWT key được cấu hình trong `appsettings.Development.json`
- DB schema được bootstrap bởi `sql/init_schema.sql`
- Frontend chạy ở port 80 (default)
