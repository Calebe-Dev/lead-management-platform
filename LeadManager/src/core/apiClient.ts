import { getAccessToken } from './authSession'
import { refreshAccessToken } from './authService'

type Primitive = string | number | boolean
type QueryValue = Primitive | undefined | null
type QueryRecord = Record<string, QueryValue>

function buildUrl(path: string, query?: QueryRecord): string {
  if (!query) {
    return path
  }

  const url = new URL(path, window.location.origin)
  Object.entries(query).forEach(([key, value]) => {
    if (value === undefined || value === null || value === '') {
      return
    }

    url.searchParams.set(key, String(value))
  })

  return `${url.pathname}${url.search}`
}

async function parseError(response: Response): Promise<string> {
  const raw = await response.text()
  if (!raw) {
    return `Request failed with status ${response.status}`
  }

  try {
    const parsed = JSON.parse(raw) as { title?: string; detail?: string; status?: number }
    return parsed.detail ?? parsed.title ?? raw
  } catch {
    return raw
  }
}

async function requestInternal<T>(
  path: string,
  init: RequestInit | undefined,
  query: QueryRecord | undefined,
  allowRefresh: boolean,
): Promise<T> {
  const token = getAccessToken()
  const response = await fetch(buildUrl(path, query), {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(init?.headers ?? {}),
    },
  })

  if (response.status === 401 && allowRefresh) {
    await refreshAccessToken()
    return requestInternal<T>(path, init, query, false)
  }

  if (!response.ok) {
    throw new Error(await parseError(response))
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

export function apiRequest<T>(path: string, init?: RequestInit, query?: QueryRecord): Promise<T> {
  return requestInternal<T>(path, init, query, true)
}
