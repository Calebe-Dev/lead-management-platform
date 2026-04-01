import { flushPromises, mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'
import LeadsModule from '../ui/LeadsModule.vue'

vi.mock('../../../core/authSession', () => ({
  getAccessToken: vi.fn(() => 'token-abc'),
}))

vi.mock('../../../core/authService', () => ({
  login: vi.fn(),
  logout: vi.fn(),
}))

vi.mock('../services/leadsService', () => ({
  listLeads: vi.fn(async () => ({
    items: [
      {
        id: 'lead-1',
        name: 'Alice',
        email: 'alice@mail.com',
        phone: '123',
        company: 'ACME',
        jobTitle: 'Manager',
        source: 'Website',
        region: 'South',
        leadType: 'SMB',
        productInterest: 'CRM',
        cnpj: '',
        assignedTo: 'ana.silva',
        campaignId: null,
        score: 80,
        temperature: 'Hot',
        status: 'Qualified',
        createdAtUtc: '2026-01-01T00:00:00Z',
        updatedAtUtc: '2026-01-01T00:00:00Z',
      },
    ],
    page: 1,
    pageSize: 10,
    totalItems: 1,
    totalPages: 1,
  })),
  createLead: vi.fn(async () => ({ id: 'lead-2' })),
  getLeadById: vi.fn(async () => ({
    id: 'lead-1',
    name: 'Alice',
    email: 'alice@mail.com',
    phone: '123',
    company: 'ACME',
    jobTitle: 'Manager',
    source: 'Website',
    region: 'South',
    leadType: 'SMB',
    productInterest: 'CRM',
    cnpj: '',
    assignedTo: 'ana.silva',
    campaignId: null,
    score: 80,
    temperature: 'Hot',
    status: 'Qualified',
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z',
  })),
  updateLeadStatus: vi.fn(async () => ({
    id: 'lead-1',
    status: 'Converted',
  })),
  recalculateLeadScore: vi.fn(async () => ({
    id: 'lead-1',
    score: 90,
  })),
  mergeLead: vi.fn(async () => ({
    id: 'lead-1',
  })),
  listLeadHistory: vi.fn(async () => ({
    items: [],
    page: 1,
    pageSize: 20,
    totalItems: 0,
    totalPages: 0,
  })),
  syncLeadToCrm: vi.fn(async () => ({
    id: 'lead-1',
  })),
}))

vi.mock('../services/campaignsService', () => ({
  listCampaigns: vi.fn(async () => []),
  createCampaign: vi.fn(),
  deleteCampaign: vi.fn(),
}))

vi.mock('../services/dashboardService', () => ({
  getDashboardOverview: vi.fn(async () => ({
    totalLeads: 1,
    newLeads: 0,
    inServiceLeads: 0,
    qualifiedLeads: 1,
    convertedLeads: 0,
    lostLeads: 0,
    averageScore: 80,
    conversionRate: 0,
    byTemperature: [],
    bySource: [],
  })),
}))

vi.mock('../services/usersService', () => ({
  listUsers: vi.fn(async () => []),
  createUser: vi.fn(),
  deleteUser: vi.fn(),
}))

describe('LeadsModule', () => {
  it('renders authenticated module with tabs and lead data', async () => {
    const wrapper = mount(LeadsModule)
    await flushPromises()

    expect(wrapper.text()).toContain('Lead Management Platform')
    expect(wrapper.text()).toContain('Leads')
    expect(wrapper.text()).toContain('Campanhas')
    expect(wrapper.text()).toContain('Dashboard')
    expect(wrapper.text()).toContain('Usuários')
    expect(wrapper.text()).toContain('Alice')
  })
})
