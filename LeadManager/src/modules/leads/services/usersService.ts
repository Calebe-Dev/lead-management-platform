import { apiRequest } from '../../../core/apiClient'
import type { CreateUserPayload, User } from '../types'

const API_BASE_URL = '/api/users'

export function listUsers(): Promise<User[]> {
  return apiRequest<User[]>(API_BASE_URL)
}

export function createUser(payload: CreateUserPayload): Promise<User> {
  return apiRequest<User>(API_BASE_URL, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function deleteUser(id: string): Promise<void> {
  return apiRequest<void>(`${API_BASE_URL}/${id}`, {
    method: 'DELETE',
  })
}
