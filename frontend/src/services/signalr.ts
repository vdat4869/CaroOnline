import * as signalR from '@microsoft/signalr'

const hubUrl = (import.meta.env.VITE_SIGNALR_HUB as string) || 'http://localhost:8080/hub/game'

export const hub = new signalR.HubConnectionBuilder()
  .withUrl(hubUrl, {
    accessTokenFactory: () => localStorage.getItem('token') || ''
  })
  .withAutomaticReconnect()
  .build()

// Log connection errors and state changes
hub.onclose((error) => {
  console.error('SignalR connection closed:', error)
  // Dispatch event để components có thể listen
  window.dispatchEvent(new CustomEvent('signalr:closed', { detail: error }))
})

hub.onreconnecting((error) => {
  console.log('SignalR reconnecting:', error)
  window.dispatchEvent(new CustomEvent('signalr:reconnecting', { detail: error }))
})

hub.onreconnected((connectionId) => {
  console.log('SignalR reconnected:', connectionId)
  window.dispatchEvent(new CustomEvent('signalr:reconnected', { detail: connectionId }))
})


