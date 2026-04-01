import { apiRequest } from '../../../core/apiClient'
import type {
  CreateLeadPayload,
  Lead,
  LeadHistoryEntry,
  ListLeadsQuery,
  MergeLeadPayload,
  PagedResponse,
  UpdateLeadStatusPayload,
} from '../types'

const API_BASE_URL = '/api/leads'

export async function listLeads(query: ListLeadsQuery = {}): Promise<PagedResponse<Lead>> {
  return apiRequest<PagedResponse<Lead>>(API_BASE_URL, undefined, query)
}

export async function createLead(payload: CreateLeadPayload): Promise<Lead> {
  return apiRequest<Lead>(API_BASE_URL, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export async function getLeadById(id: string): Promise<Lead> {
  return apiRequest<Lead>(`${API_BASE_URL}/${id}`)
}

export async function updateLeadStatus(id: string, payload: UpdateLeadStatusPayload): Promise<Lead> {
  return apiRequest<Lead>(`${API_BASE_URL}/${id}/status`, {
    method: 'PATCH',
    body: JSON.stringify(payload),
  })
}

export async function recalculateLeadScore(id: string): Promise<Lead> {
  return apiRequest<Lead>(`${API_BASE_URL}/${id}/score`, {
    method: 'POST',
    body: JSON.stringify({}),
  })
}

export async function mergeLead(targetLeadId: string, payload: MergeLeadPayload): Promise<Lead> {
  return apiRequest<Lead>(`${API_BASE_URL}/${targetLeadId}/merge`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export async function listLeadHistory(
  leadId: string,
  page = 1,
  pageSize = 20,
): Promise<PagedResponse<LeadHistoryEntry>> {
  return apiRequest<PagedResponse<LeadHistoryEntry>>(`${API_BASE_URL}/${leadId}/history`, undefined, { page, pageSize })
}

export async function syncLeadToCrm(leadId: string): Promise<Lead> {
  return apiRequest<Lead>(`/api/integrations/crm/sync/${leadId}`, {
    method: 'POST',
    body: JSON.stringify({}),
  })
}
