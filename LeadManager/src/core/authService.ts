import { clearSession, getRefreshToken, setSession } from './authSession'
import type { AuthTokenResponse } from '../modules/leads/types'

export interface LoginPayload {
  username: string
  password: string
}

async function parseError(response: Response): Promise<string> {
  const raw = await response.text()
  if (!raw) {
    return `Request failed with status ${response.status}`
  }

  try {
    const parsed = JSON.parse(raw) as { detail?: string; title?: string }
    return parsed.detail ?? parsed.title ?? raw
  } catch {
    return raw
  }
}

export async function login(payload: LoginPayload): Promise<AuthTokenResponse> {
  const response = await fetch('/api/auth/token', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(payload),
  })

  if (!response.ok) {
    throw new Error(await parseError(response))
  }

  const token = (await response.json()) as AuthTokenResponse
  setSession(token)
  return token
}

export async function refreshAccessToken(): Promise<AuthTokenResponse> {
  const refreshToken = getRefreshToken()
  if (!refreshToken) {
    throw new Error('No refresh token available')
  }

  const response = await fetch('/api/auth/refresh', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ refreshToken }),
  })

  if (!response.ok) {
    clearSession()
    throw new Error(await parseError(response))
  }

  const token = (await response.json()) as AuthTokenResponse
  setSession(token)
  return token
}

export async function logout(): Promise<void> {
  const refreshToken = getRefreshToken()
  if (!refreshToken) {
    clearSession()
    return
  }

  try {
    await fetch('/api/auth/logout', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ refreshToken }),
    })
  } finally {
    clearSession()
  }
}
