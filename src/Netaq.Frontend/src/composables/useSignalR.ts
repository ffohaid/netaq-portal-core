import { ref, onUnmounted } from 'vue'
import * as signalR from '@microsoft/signalr'

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000'

const connection = ref<signalR.HubConnection | null>(null)
const isConnected = ref(false)

export function useSignalR() {
  function connect() {
    const token = localStorage.getItem('netaq_access_token')
    if (!token) return

    connection.value = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/notifications`, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connection.value.start()
      .then(() => {
        isConnected.value = true
        console.log('SignalR connected')
      })
      .catch((err) => {
        console.error('SignalR connection error:', err)
      })

    connection.value.onreconnected(() => {
      isConnected.value = true
    })

    connection.value.onclose(() => {
      isConnected.value = false
    })
  }

  function on(eventName: string, callback: (...args: any[]) => void) {
    connection.value?.on(eventName, callback)
  }

  function off(eventName: string) {
    connection.value?.off(eventName)
  }

  function disconnect() {
    connection.value?.stop()
    isConnected.value = false
  }

  onUnmounted(() => {
    // Do not disconnect on component unmount - keep global connection
  })

  return {
    connection,
    isConnected,
    connect,
    on,
    off,
    disconnect,
  }
}
