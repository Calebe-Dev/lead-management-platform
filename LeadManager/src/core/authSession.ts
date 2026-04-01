import type { AuthTokenResponse } from '../modules/leads/types'

const AUTH_STORAGE_KEY = 'lead_manager_auth_session'

interface AuthSessionData {
  accessToken: string
  refreshToken: string
}

let inMemorySession: AuthSessionData | null = null

function isBrowser(): boolean {
  return typeof window !== 'undefined'
}

function readFromStorage(): AuthSessionData | null {
  if (!isBrowser()) {
    return null
  }

  const raw = window.localStorage.getItem(AUTH_STORAGE_KEY)
  if (!raw) {
    return null
  }

  try {
    const parsed = JSON.parse(raw) as AuthSessionData
    if (!parsed.accessToken || !parsed.refreshToken) {
      return null
    }

    return parsed
  } catch {
    return null
  }
}

function writeToStorage(data: AuthSessionData | null): void {
  if (!isBrowser()) {
    return
  }

  if (!data) {
    window.localStorage.removeItem(AUTH_STORAGE_KEY)
    return
  }

  window.localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(data))
}

export function getAccessToken(): string {
  if (!inMemorySession) {
    inMemorySession = readFromStorage()
  }

  return inMemorySession?.accessToken ?? ''
}

export function getRefreshToken(): string {
  if (!inMemorySession) {
    inMemorySession = readFromStorage()
  }

  return inMemorySession?.refreshToken ?? ''
}

export function setSession(tokenResponse: AuthTokenResponse): void {
  const nextSession = {
    accessToken: tokenResponse.accessToken,
    refreshToken: tokenResponse.refreshToken,
  }
  inMemorySession = nextSession
  writeToStorage(nextSession)
}

export function clearSession(): void {
  inMemorySession = null
  writeToStorage(null)
}
