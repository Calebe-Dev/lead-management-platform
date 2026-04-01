import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  createLead,
  getLeadById,
  listLeadHistory,
  listLeads,
  mergeLead,
  recalculateLeadScore,
  syncLeadToCrm,
  updateLeadStatus,
} from '../services/leadsService'

describe('leadsService', () => {
  afterEach(() => {
    window.localStorage.clear()
    vi.unstubAllGlobals()
  })

  it('calls paged list endpoint with query params', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ items: [], page: 1, pageSize: 10, totalItems: 0, totalPages: 0 }),
    }))

    await listLeads({ search: 'john', page: 2, pageSize: 5 })

    expect(fetch).toHaveBeenCalledWith(
      '/api/leads?search=john&page=2&pageSize=5',
      expect.objectContaining({
        headers: expect.objectContaining({ 'Content-Type': 'application/json' }),
      }),
    )
  })

  it('creates lead with required extended payload', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: true,
      status: 201,
      json: async () => ({ id: '1' }),
    }))

    await createLead({
      name: 'Lead',
      email: 'lead@example.com',
      phone: '5511999999999',
      company: 'ACME',
      jobTitle: 'CEO',
      source: 'organic',
      region: 'South',
      leadType: 'Enterprise',
      productInterest: 'CRM',
      cnpj: '12345678000190',
    })

    expect(fetch).toHaveBeenCalledWith('/api/leads', expect.objectContaining({ method: 'POST' }))
  })

  it('adds bearer token from auth session', async () => {
    window.localStorage.setItem(
      'lead_manager_auth_session',
      JSON.stringify({ accessToken: 'token-abc', refreshToken: 'refresh-abc' }),
    )
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ items: [], page: 1, pageSize: 10, totalItems: 0, totalPages: 0 }),
    }))

    await listLeads()

    expect(fetch).toHaveBeenCalledWith('/api/leads', expect.objectContaining({
      headers: expect.objectContaining({
        Authorization: 'Bearer token-abc',
      }),
    }))
  })

  it('calls lead action endpoints', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ id: 'lead-1', items: [] }),
    }))

    await getLeadById('lead-1')
    await updateLeadStatus('lead-1', { status: 'Qualified' })
    await recalculateLeadScore('lead-1')
    await mergeLead('lead-1', { sourceLeadId: 'lead-2', precedence: 'Target' })
    await listLeadHistory('lead-1')
    await syncLeadToCrm('lead-1')

    expect(fetch).toHaveBeenNthCalledWith(1, '/api/leads/lead-1', expect.any(Object))
    expect(fetch).toHaveBeenNthCalledWith(2, '/api/leads/lead-1/status', expect.objectContaining({ method: 'PATCH' }))
    expect(fetch).toHaveBeenNthCalledWith(3, '/api/leads/lead-1/score', expect.objectContaining({ method: 'POST' }))
    expect(fetch).toHaveBeenNthCalledWith(4, '/api/leads/lead-1/merge', expect.objectContaining({ method: 'POST' }))
    expect(fetch).toHaveBeenNthCalledWith(5, '/api/leads/lead-1/history?page=1&pageSize=20', expect.any(Object))
    expect(fetch).toHaveBeenNthCalledWith(6, '/api/integrations/crm/sync/lead-1', expect.objectContaining({ method: 'POST' }))
  })
})
