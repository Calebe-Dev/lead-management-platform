import type { CreateLeadPayload, Lead, UpdateLeadStatusPayload } from '../types'

const API_BASE_URL = '/api/leads'

async function parseError(response: Response): Promise<string> {
  const message = await response.text()
  return message || `Request failed with status ${response.status}`
}

async function request<T>(input: RequestInfo | URL, init?: RequestInit): Promise<T> {
  const response = await fetch(input, {
    headers: {
      'Content-Type': 'application/json',
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
