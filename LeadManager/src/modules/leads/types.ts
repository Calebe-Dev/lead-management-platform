export interface Lead {
  id: string
  name: string
  email: string
  phone: string
  company: string
  jobTitle: string
  source: string
  score: number
  temperature: string
  status: LeadStatus
  createdAtUtc: string
}

export interface CreateLeadPayload {
  name: string
  email: string
  phone: string
  company: string
  jobTitle: string
  source: string
}

export type LeadStatus = 'New' | 'InService' | 'Qualified' | 'Converted' | 'Lost'

export interface UpdateLeadStatusPayload {
  status: LeadStatus
}
