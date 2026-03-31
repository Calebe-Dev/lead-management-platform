import { flushPromises, mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'
import LeadsModule from '../ui/LeadsModule.vue'

vi.mock('../services/leadsService', () => ({
  listLeads: vi.fn(async () => [
    {
      id: 'lead-1',
      name: 'Alice',
      email: 'alice@mail.com',
      phone: '123',
      company: 'ACME',
      jobTitle: 'Manager',
      source: 'Website',
      score: 80,
      temperature: 'Hot',
      status: 'Qualified',
      createdAtUtc: '2026-01-01T00:00:00Z',
    },
  ]),
  createLead: vi.fn(async () => ({
    id: 'lead-2',
    name: 'Bob',
    email: 'bob@mail.com',
    phone: '456',
    company: 'Beta',
    jobTitle: 'Owner',
    source: 'Referral',
    score: 50,
    temperature: 'Warm',
    status: 'New',
    createdAtUtc: '2026-01-02T00:00:00Z',
  })),
  getLeadById: vi.fn(async () => ({
    id: 'lead-1',
    name: 'Alice',
    email: 'alice@mail.com',
    phone: '123',
    company: 'ACME',
    jobTitle: 'Manager',
    source: 'Website',
    score: 80,
    temperature: 'Hot',
    status: 'Qualified',
    createdAtUtc: '2026-01-01T00:00:00Z',
  })),
  updateLeadStatus: vi.fn(async () => ({
    id: 'lead-1',
    name: 'Alice',
    email: 'alice@mail.com',
    phone: '123',
    company: 'ACME',
    jobTitle: 'Manager',
    source: 'Website',
    score: 82,
    temperature: 'Hot',
    status: 'Converted',
    createdAtUtc: '2026-01-01T00:00:00Z',
  })),
  recalculateLeadScore: vi.fn(async () => ({
    id: 'lead-1',
    name: 'Alice',
    email: 'alice@mail.com',
    phone: '123',
    company: 'ACME',
    jobTitle: 'Manager',
    source: 'Website',
    score: 85,
    temperature: 'Hot',
    status: 'Qualified',
    createdAtUtc: '2026-01-01T00:00:00Z',
  })),
}))

describe('LeadsModule', () => {
  it('renders fetched lead and supports load by id flow', async () => {
    const wrapper = mount(LeadsModule)
    await flushPromises()

    expect(wrapper.text()).toContain('Lead Management')
    expect(wrapper.text()).toContain('Alice')

    await wrapper.find('form.row input').setValue('lead-1')
    await wrapper.find('form.row').trigger('submit.prevent')
    await flushPromises()

    expect(wrapper.text()).toContain('Status: Qualified')
  })
})
