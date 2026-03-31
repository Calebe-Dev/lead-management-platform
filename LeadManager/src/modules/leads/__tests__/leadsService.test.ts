import { afterEach, describe, expect, it, vi } from 'vitest'
import { createLead, getLeadById, listLeads, recalculateLeadScore, updateLeadStatus } from '../services/leadsService'

describe('leadsService', () => {
  afterEach(() => {
    window.localStorage.clear()
    vi.unstubAllGlobals()
  })

  it('calls list endpoint', async () => {
    const response = [{ id: '1' }]
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => response,
    }))

    const result = await listLeads()

    expect(fetch).toHaveBeenCalledWith('/api/leads', expect.objectContaining({
      headers: expect.objectContaining({
        'Content-Type': 'application/json',
      }),
    }))
    expect(result).toEqual(response)
  })

  it('calls create endpoint', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: true,
      status: 201,
      json: async () => ({ id: '2' }),
    }))

    await createLead({
      name: 'A',
      email: 'a@a.com',
      phone: '1',
      company: 'C',
      jobTitle: 'JT',
      source: 'Web',
    })

    expect(fetch).toHaveBeenCalledWith('/api/leads', expect.objectContaining({
      method: 'POST',
      headers: expect.objectContaining({
        'Content-Type': 'application/json',
      }),
    }))
  })

  it('adds bearer token when available', async () => {
    window.localStorage.setItem('lead_manager_token', 'test-token')
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => [{ id: '1' }],
    }))

    await listLeads()

    expect(fetch).toHaveBeenCalledWith('/api/leads', expect.objectContaining({
      headers: expect.objectContaining({
        Authorization: 'Bearer test-token',
      }),
    }))
  })

  it('calls detail, status and score endpoints', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ id: '3' }),
    }))

    await getLeadById('3')
    await updateLeadStatus('3', { status: 'Qualified' })
    await recalculateLeadScore('3')

    expect(fetch).toHaveBeenNthCalledWith(1, '/api/leads/3', expect.any(Object))
    expect(fetch).toHaveBeenNthCalledWith(
      2,
      '/api/leads/3/status',
      expect.objectContaining({ method: 'PATCH' }),
    )
    expect(fetch).toHaveBeenNthCalledWith(3, '/api/leads/3/score', expect.objectContaining({ method: 'POST' }))
  })
})
