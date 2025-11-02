import React, { useEffect, useState } from 'react'
import * as signalR from '@microsoft/signalr'
import { hub } from '../services/signalr'
import { Link } from 'react-router-dom'

export default function Lobby() {
  const [connected, setConnected] = useState(false)
  const [usersOnline, setUsersOnline] = useState<number>(0)

  useEffect(() => {
    let mounted = true
    let cleanupDone = false
    
    // Kiểm tra token trước
    const token = localStorage.getItem('token')
    if (!token) {
      setConnected(false)
      return () => { mounted = false }
    }
    
    const startConnection = async () => {
      try {
        // Nếu đã connected rồi thì chỉ join lobby
        if (hub.state === signalR.HubConnectionState.Connected) {
          if (!mounted || cleanupDone) return
          setConnected(true)
          await hub.invoke('JoinLobby')
          return
        }

        // Nếu đang trong các state khác (Connecting, Reconnecting), đợi nó ready
        if (hub.state !== signalR.HubConnectionState.Disconnected) {
          const checkInterval = setInterval(() => {
            if (hub.state === signalR.HubConnectionState.Connected) {
              clearInterval(checkInterval)
              if (mounted && !cleanupDone) {
                setConnected(true)
                hub.invoke('JoinLobby').catch(() => {})
              }
            } else if (hub.state === signalR.HubConnectionState.Disconnected) {
              clearInterval(checkInterval)
              setTimeout(() => {
                if (mounted && !cleanupDone) {
                  startConnection()
                }
              }, 500)
            }
          }, 200)
          setTimeout(() => clearInterval(checkInterval), 3000)
          return
        }

        // Chỉ start nếu đang Disconnected
        await hub.start()
        if (!mounted || cleanupDone) return
        
        const finalState = hub.state as signalR.HubConnectionState
        if (finalState === signalR.HubConnectionState.Connected) {
          setConnected(true)
          await hub.invoke('JoinLobby')
        } else {
          // Đợi connection ready
          const waitInterval = setInterval(() => {
            const currentState = hub.state as signalR.HubConnectionState
            if (currentState === signalR.HubConnectionState.Connected) {
              clearInterval(waitInterval)
              if (mounted && !cleanupDone) {
                setConnected(true)
                hub.invoke('JoinLobby').catch(() => {})
              }
            } else if (currentState === signalR.HubConnectionState.Disconnected) {
              clearInterval(waitInterval)
            }
          }, 200)
          setTimeout(() => clearInterval(waitInterval), 2000)
        }
      } catch (error: any) {
        // Bỏ qua lỗi nếu connection đã được start ở nơi khác (race condition với React.StrictMode)
        if (error?.message?.includes('not in the \'Disconnected\' state')) {
          const checkInterval = setInterval(() => {
            if (hub.state === signalR.HubConnectionState.Connected) {
              clearInterval(checkInterval)
              if (mounted && !cleanupDone) {
                setConnected(true)
                hub.invoke('JoinLobby').catch(() => {})
              }
            } else if (hub.state === signalR.HubConnectionState.Disconnected) {
              clearInterval(checkInterval)
              setTimeout(() => {
                if (mounted && !cleanupDone) {
                  startConnection()
                }
              }, 500)
            }
          }, 200)
          setTimeout(() => clearInterval(checkInterval), 3000)
          return
        }
        
        // Các lỗi khác
        if (!mounted || cleanupDone) return
        setConnected(false)
        setTimeout(() => {
          if (mounted && !cleanupDone) {
            startConnection()
          }
        }, 500)
      }
    }

    startConnection().catch(() => {})
    
    // Listen for connection state changes via SignalR events
    const updateConnectionState = () => {
      if (mounted && !cleanupDone) {
        const isConnected = hub.state === signalR.HubConnectionState.Connected
        setConnected(isConnected)
      }
    }

    // Subscribe to connection state events
    hub.onclose(updateConnectionState)
    hub.onreconnecting(updateConnectionState)
    hub.onreconnected(() => {
      updateConnectionState()
      if (mounted && !cleanupDone) {
        hub.invoke('JoinLobby').catch(() => {})
      }
    })

    // Listen to custom events from signalr.ts
    const handleReconnected = () => {
      updateConnectionState()
    }
    window.addEventListener('signalr:reconnected', handleReconnected)

    // Listen to hub events
    hub.on('LobbyUpdate', (payload: any) => {
      if (mounted && !cleanupDone) {
        setUsersOnline(payload?.usersOnline ?? 0)
      }
    })

    // Poll connection state để đảm bảo UI được update (poll mỗi 300ms để giảm overhead)
    const stateCheckInterval = setInterval(() => {
      if (mounted && !cleanupDone) {
        const currentState = hub.state as signalR.HubConnectionState
        const isConnected = currentState === signalR.HubConnectionState.Connected
        setConnected(prev => {
          if (prev !== isConnected && isConnected) {
            // Nếu vừa connect, thử join lobby
            hub.invoke('JoinLobby').catch(() => {})
          }
          return isConnected
        })
      }
    }, 300)

    return () => {
      cleanupDone = true
      mounted = false
      clearInterval(stateCheckInterval)
      hub.off('LobbyUpdate')
      window.removeEventListener('signalr:reconnected', handleReconnected)
    }
  }, [])

  return (
    <div className="space-y-3">
      <div className="text-lg">Lobby</div>
      <div>Status: {connected ? 'Connected' : 'Disconnected'}</div>
      <div>Users online: {usersOnline}</div>
      <div className="space-x-2">
        <button 
          onClick={async () => {
            try {
              const result: any = await hub.invoke('CreateGame', 'PvP', null, null, null, 600)
              const gameId = result?.Id || result?.id
              if (gameId) {
                window.location.href = `/game/${gameId}`
              }
            } catch (error) {
              alert('Failed to create game')
            }
          }}
          className="px-3 py-1 bg-emerald-600 text-white rounded"
        >
          Create PvP Game
        </button>
        <button 
          onClick={async () => {
            try {
              const result: any = await hub.invoke('CreateGame', 'PvE', null, null, 1, 600)
              const gameId = result?.Id || result?.id
              if (gameId) {
                window.location.href = `/game/${gameId}`
              }
            } catch (error) {
              alert('Failed to create PvE game')
            }
          }}
          className="px-3 py-1 bg-blue-600 text-white rounded"
        >
          Create PvE Game
        </button>
      </div>
    </div>
  )
}


