import { apiRequest } from '../../../core/apiClient'
import type { DashboardOverview } from '../types'

export function getDashboardOverview(): Promise<DashboardOverview> {
  return apiRequest<DashboardOverview>('/api/dashboard/overview')
}
