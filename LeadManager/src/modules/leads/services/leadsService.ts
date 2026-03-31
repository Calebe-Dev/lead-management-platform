import type { CreateLeadPayload, Lead, UpdateLeadStatusPayload } from '../types'

const API_BASE_URL = '/api/leads'
const AUTH_TOKEN_KEY = 'lead_manager_token'

async function parseError(response: Response): Promise<string> {
  const message = await response.text()
  return message || `Request failed with status ${response.status}`
}

function getAuthHeaders(): Record<string, string> {
  if (typeof window === 'undefined') {
    return {}
  }

  const token = window.localStorage.getItem(AUTH_TOKEN_KEY)
  return token ? { Authorization: `Bearer ${token}` } : {}
}

async function request<T>(input: RequestInfo | URL, init?: RequestInit): Promise<T> {
  const response = await fetch(input, {
    headers: {
      'Content-Type': 'application/json',
      ...getAuthHeaders(),
      ...(init?.headers ?? {}),
    },
    ...init,
  })

  if (!response.ok) {
    throw new Error(await parseError(response))
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

export async function listLeads(): Promise<Lead[]> {
  return request<Lead[]>(API_BASE_URL)
}

export async function createLead(payload: CreateLeadPayload): Promise<Lead> {
  return request<Lead>(API_BASE_URL, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export async function getLeadById(id: string): Promise<Lead> {
  return request<Lead>(`${API_BASE_URL}/${id}`)
}

export async function updateLeadStatus(id: string, payload: UpdateLeadStatusPayload): Promise<Lead> {
  return request<Lead>(`${API_BASE_URL}/${id}/status`, {
    method: 'PATCH',
    body: JSON.stringify(payload),
  })
}

export async function recalculateLeadScore(id: string): Promise<Lead> {
  return request<Lead>(`${API_BASE_URL}/${id}/score`, {
    method: 'POST',
    body: JSON.stringify({}),
  })
}
