import { apiRequest } from '../../../core/apiClient'
import type { Campaign, UpsertCampaignPayload } from '../types'

const API_BASE_URL = '/api/campaigns'

export function listCampaigns(): Promise<Campaign[]> {
  return apiRequest<Campaign[]>(API_BASE_URL)
}

export function createCampaign(payload: UpsertCampaignPayload): Promise<Campaign> {
  return apiRequest<Campaign>(API_BASE_URL, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function updateCampaign(id: string, payload: UpsertCampaignPayload): Promise<Campaign> {
  return apiRequest<Campaign>(`${API_BASE_URL}/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })
}

export function deleteCampaign(id: string): Promise<void> {
  return apiRequest<void>(`${API_BASE_URL}/${id}`, {
    method: 'DELETE',
  })
}
