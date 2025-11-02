import React, { useEffect, useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'
import * as signalR from '@microsoft/signalr'
import { api } from '../services/api'
import { hub } from '../services/signalr'

const SIZE = 15

export default function GameBoard() {
  const { id } = useParams()
  const gameId = Number(id)
  const [board, setBoard] = useState<number[][]>(Array.from({ length: SIZE }, () => Array(SIZE).fill(0)))
  const [currentPlayer, setCurrentPlayer] = useState(1)
  const [moves, setMoves] = useState<any[]>([])
  const [connectionReady, setConnectionReady] = useState(false)
  const [isMakingMove, setIsMakingMove] = useState(false)
  const [gameMode, setGameMode] = useState<string>('PvP')
  const [gameFinished, setGameFinished] = useState<{ winner?: number; result?: string } | null>(null)

  // Đảm bảo SignalR connection được start
  useEffect(() => {
    let mounted = true
    
    const ensureConnection = async () => {
      try {
        if (hub.state === signalR.HubConnectionState.Disconnected) {
          await hub.start()
        }
        if (mounted && hub.state === signalR.HubConnectionState.Connected) {
          setConnectionReady(true)
          // Join game group để nhận events
          await hub.invoke('JoinGame', gameId).catch(() => {})
        }
      } catch (error: any) {
        if (error?.message?.includes('not in the \'Disconnected\' state')) {
          // Connection đang được start ở nơi khác, đợi nó ready
          const checkConnection = setInterval(() => {
            if (hub.state === signalR.HubConnectionState.Connected) {
              clearInterval(checkConnection)
              if (mounted) setConnectionReady(true)
            }
          }, 100)
          return () => clearInterval(checkConnection)
        }
      }
    }

    ensureConnection()

    // Poll connection state để đảm bảo state được update (poll mỗi 200ms để giảm overhead)
    const connectionCheckInterval = setInterval(() => {
      if (mounted) {
        const isConnected = hub.state === signalR.HubConnectionState.Connected
        if (isConnected) setConnectionReady(true)
      }
    }, 200)

    return () => {
      mounted = false
      clearInterval(connectionCheckInterval)
    }
  }, [gameId])

  useEffect(() => {
    (async () => {
      try {
        // Lấy game info để biết mode
        const gameRes = await api.get(`/api/games/${gameId}`)
        if (gameRes.data?.mode) {
          setGameMode(gameRes.data.mode)
        }
        const res = await api.get(`/api/games/${gameId}/moves`)
        const ms = res.data as any[]
        setMoves(ms)
        const b = Array.from({ length: SIZE }, () => Array(SIZE).fill(0))
        for (const m of ms) b[m.x][m.y] = m.player
        setBoard(b)
        setCurrentPlayer((ms.length % 2) + 1)
      } catch (error: any) {
        // Nếu game không tồn tại, ignore (có thể tạo game mới)
      }
    })()
  }, [gameId])

  useEffect(() => {
    const moveHandler = (payload: any) => {
      if (payload.gameId !== gameId) return
      const { x, y, player } = payload
      setBoard(prev => {
        const copy = prev.map(r => r.slice())
        copy[x][y] = player
        return copy
      })
      setCurrentPlayer(player === 1 ? 2 : 1)
      setMoves(prev => [...prev, payload])
    }
    
    const gameEndedHandler = (payload: any) => {
      if (payload.gameId !== gameId) return
      setGameFinished({ winner: payload.winner, result: payload.result })
      if (payload.winner === 1 || payload.result?.includes('P1_WIN')) {
        alert('Player 1 thắng!')
      } else if (payload.winner === 2 || payload.result?.includes('P2_WIN')) {
        alert(gameMode === 'PvE' ? 'AI thắng!' : 'Player 2 thắng!')
      } else {
        alert('Game kết thúc!')
      }
    }
    
    hub.on('MoveMade', moveHandler)
    hub.on('GameEnded', gameEndedHandler)
    return () => { 
      hub.off('MoveMade', moveHandler as any)
      hub.off('GameEnded', gameEndedHandler as any)
    }
  }, [gameId, gameMode])

  async function handleCellClick(x: number, y: number) {
    // Prevent double-click và moves khi đang process
    if (board[x][y] !== 0 || isMakingMove) return
    
    // Nếu connection chưa ready, chỉ check nhanh, không block
    if (!connectionReady) {
      if (hub.state !== signalR.HubConnectionState.Connected) {
        alert('Đang kết nối, vui lòng đợi...')
        return
      }
      setConnectionReady(true)
    }
    
    setIsMakingMove(true)
    // Optimistic UI update ngay lập tức để responsive hơn
    setBoard(prev => {
      const copy = prev.map(r => r.slice())
      copy[x][y] = currentPlayer
      return copy
    })
    
    try {
      await hub.invoke('MakeMove', gameId, x, y)
      // UI sẽ được update qua MoveMade event từ server (nếu chưa được update optimistic)
    } catch (error: any) {
      // SignalR trả về error trong nhiều format khác nhau
      // Thử extract từ các property khác nhau
      let errorMsg = ''
      if (typeof error === 'string') {
        errorMsg = error
      } else if (error?.error) {
        errorMsg = typeof error.error === 'string' ? error.error : error.error?.message || JSON.stringify(error.error)
      } else if (error?.message) {
        errorMsg = error.message
      } else if (error?.toString) {
        errorMsg = error.toString()
      } else {
        errorMsg = 'Failed to make move'
      }
      
      // Parse error từ SignalR HubException
      let displayMsg = errorMsg
      const errorMsgLower = errorMsg.toLowerCase()
      if (errorMsgLower.includes('game not found')) {
        displayMsg = `Game ${gameId} không tồn tại. Vui lòng tạo game mới từ lobby.`
      } else if (errorMsgLower.includes('cell already occupied') || errorMsgLower.includes('already occupied')) {
        displayMsg = 'Ô này đã có quân cờ. Vui lòng chọn ô khác.'
      } else if (errorMsgLower.includes('not your turn') || errorMsgLower.includes('not your turn')) {
        displayMsg = 'Chưa đến lượt của bạn.'
      } else if (errorMsgLower.includes('game already finished') || errorMsgLower.includes('already finished')) {
        displayMsg = 'Game đã kết thúc.'
      } else if (errorMsgLower.includes('out of bounds') || errorMsgLower.includes('out of bounds')) {
        displayMsg = 'Vị trí không hợp lệ.'
      } else if (errorMsgLower.includes('not in the \'connected\' state')) {
        displayMsg = 'Connection lost. Vui lòng refresh trang.'
      } else {
        displayMsg = `Lỗi: ${errorMsg}`
      }
      
      alert(displayMsg)
      // Reload board state từ server nếu có lỗi
      try {
        const res = await api.get(`/api/games/${gameId}/moves`)
        const ms = res.data as any[]
        const b = Array.from({ length: SIZE }, () => Array(SIZE).fill(0))
        for (const m of ms) b[m.x][m.y] = m.player
        setBoard(b)
        setCurrentPlayer((ms.length % 2) + 1)
      } catch (e) {
        // Ignore reload error
      }
    } finally {
      setIsMakingMove(false)
    }
  }

  const grid = useMemo(() => {
    // Trong PvE mode, disable nếu đến lượt AI hoặc game đã kết thúc
    const isAiTurn = gameMode === 'PvE' && currentPlayer === 2
    const isGameFinished = gameFinished !== null
    return (
      <div className="inline-grid" style={{ gridTemplateColumns: `repeat(${SIZE}, 32px)` }}>
        {Array.from({ length: SIZE * SIZE }).map((_, i) => {
          const x = Math.floor(i / SIZE)
          const y = i % SIZE
          const v = board[x][y]
          return (
            <button 
              key={i} 
              onClick={() => handleCellClick(x, y)} 
              disabled={isMakingMove || v !== 0 || isAiTurn || isGameFinished}
              className={`w-8 h-8 border flex items-center justify-center ${
                isMakingMove || isAiTurn || isGameFinished ? 'bg-gray-200 cursor-not-allowed' : 
                v !== 0 ? 'bg-amber-100 cursor-not-allowed' : 
                'bg-amber-50 hover:bg-amber-100 cursor-pointer'
              }`}
            >
              {v === 1 ? 'X' : v === 2 ? 'O' : ''}
            </button>
          )
        })}
      </div>
    )
  }, [board, gameMode, currentPlayer, isMakingMove, gameFinished])

  return (
    <div className="space-y-4">
      <div className="text-lg">Game #{gameId}</div>
      {gameFinished ? (
        <div className="text-green-600 text-lg font-bold">
          {gameFinished.winner === 1 ? 'Player 1 thắng!' : gameMode === 'PvE' ? 'AI thắng!' : 'Player 2 thắng!'}
        </div>
      ) : (
        <div>Turn: Player {currentPlayer} {gameMode === 'PvE' && currentPlayer === 2 ? '(AI)' : ''}</div>
      )}
      {!connectionReady && (
        <div className="text-yellow-600 text-sm">Connecting to game server...</div>
      )}
      {gameMode === 'PvE' && currentPlayer === 2 && !gameFinished && (
        <div className="text-blue-600 text-sm">AI đang suy nghĩ...</div>
      )}
      {grid}
    </div>
  )
}


