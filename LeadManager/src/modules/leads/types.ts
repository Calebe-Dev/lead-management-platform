export type LeadStatus = 'New' | 'InService' | 'Qualified' | 'Converted' | 'Lost'
export type LeadTemperature = 'Cold' | 'Warm' | 'Hot'
export type UserRole = 'admin' | 'marketing' | 'vendas'

export interface Lead {
  id: string
  name: string
  email: string
  phone: string
  company: string
  jobTitle: string
  source: string
  region: string
  leadType: string
  productInterest: string
  cnpj: string
  assignedTo: string
  campaignId?: string | null
  score: number
  temperature: LeadTemperature
  status: LeadStatus
  createdAtUtc: string
  updatedAtUtc: string
}

export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

export interface CreateLeadPayload {
  name: string
  email: string
  phone: string
  company: string
  jobTitle: string
  source: string
  region: string
  leadType?: string
  productInterest?: string
  cnpj?: string
  campaignId?: string | null
}

export interface UpdateLeadStatusPayload {
  status: LeadStatus
}

export interface MergeLeadPayload {
  sourceLeadId: string
  precedence: 'Target' | 'Source'
}

export interface ListLeadsQuery {
  status?: LeadStatus
  temperature?: LeadTemperature
  region?: string
  leadType?: string
  productInterest?: string
  assignedTo?: string
  search?: string
  campaignId?: string
  minScore?: number
  maxScore?: number
  page?: number
  pageSize?: number
}

export interface LeadHistoryEntry {
  id: string
  leadId: string
  eventType: string
  fieldName: string
  oldValue: string
  newValue: string
  changedAtUtc: string
}

export interface Campaign {
  id: string
  name: string
  channel: string
  utm: string
  isActive: boolean
  createdAtUtc: string
  updatedAtUtc: string
}

export interface UpsertCampaignPayload {
  name: string
  channel: string
  utm: string
  isActive: boolean
}

export interface User {
  id: string
  username: string
  role: UserRole
  createdAtUtc: string
  updatedAtUtc: string
}

export interface CreateUserPayload {
  username: string
  password: string
  role: UserRole
}

export interface DashboardDimension {
  name: string
  count: number
}

export interface DashboardOverview {
  totalLeads: number
  newLeads: number
  inServiceLeads: number
  qualifiedLeads: number
  convertedLeads: number
  lostLeads: number
  averageScore: number
  conversionRate: number
  byTemperature: DashboardDimension[]
  bySource: DashboardDimension[]
}

export interface AuthTokenResponse {
  accessToken: string
  accessTokenExpiresAtUtc: string
  refreshToken: string
  refreshTokenExpiresAtUtc: string
}
