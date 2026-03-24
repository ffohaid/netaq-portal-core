import axios from 'axios'
import type { ApiResponse, TokenResponse } from '../types'

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000'

const api = axios.create({
  baseURL: `${API_BASE_URL}/api`,
  headers: {
    'Content-Type': 'application/json',
  },
})

// Request interceptor: attach JWT token
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('netaq_access_token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => Promise.reject(error)
)

// Response interceptor: handle token refresh
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true

      const refreshToken = localStorage.getItem('netaq_refresh_token')
      if (refreshToken) {
        try {
          const response = await axios.post<ApiResponse<TokenResponse>>(
            `${API_BASE_URL}/api/auth/refresh-token`,
            { refreshToken }
          )

          if (response.data.isSuccess && response.data.data) {
            localStorage.setItem('netaq_access_token', response.data.data.accessToken)
            localStorage.setItem('netaq_refresh_token', response.data.data.refreshToken)
            originalRequest.headers.Authorization = `Bearer ${response.data.data.accessToken}`
            return api(originalRequest)
          }
        } catch {
          // Refresh failed, redirect to login
          localStorage.removeItem('netaq_access_token')
          localStorage.removeItem('netaq_refresh_token')
          window.location.href = '/auth/login'
        }
      } else {
        window.location.href = '/auth/login'
      }
    }

    return Promise.reject(error)
  }
)

export default api
