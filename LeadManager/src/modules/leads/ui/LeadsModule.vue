<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { getAccessToken } from '../../../core/authSession'
import { login, logout } from '../../../core/authService'
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
import { createCampaign, deleteCampaign, listCampaigns } from '../services/campaignsService'
import { getDashboardOverview } from '../services/dashboardService'
import { createUser, deleteUser, listUsers } from '../services/usersService'
import type {
  Campaign,
  CreateLeadPayload,
  CreateUserPayload,
  DashboardOverview,
  Lead,
  LeadHistoryEntry,
  LeadStatus,
  ListLeadsQuery,
  PagedResponse,
  User,
  UserRole,
} from '../types'

type Tab = 'leads' | 'campaigns' | 'dashboard' | 'users'

const statuses: LeadStatus[] = ['New', 'InService', 'Qualified', 'Converted', 'Lost']
const roles: UserRole[] = ['admin', 'marketing', 'vendas']

const activeTab = ref<Tab>('leads')
const authenticated = ref(false)
const errorMessage = ref('')
const loading = ref(false)

const loginForm = reactive({
  username: 'admin',
  password: 'admin123!',
})

const leadFilters = reactive<ListLeadsQuery>({
  page: 1,
  pageSize: 10,
  search: '',
  status: undefined,
  region: '',
})

const leadPage = ref<PagedResponse<Lead>>({
  items: [],
  page: 1,
  pageSize: 10,
  totalItems: 0,
  totalPages: 0,
})
const selectedLeadId = ref('')
const selectedLead = ref<Lead | null>(null)
const selectedStatus = ref<LeadStatus>('New')
const mergeSourceLeadId = ref('')
const leadHistory = ref<LeadHistoryEntry[]>([])

const createLeadForm = reactive<CreateLeadPayload>({
  name: '',
  email: '',
  phone: '',
  company: '',
  jobTitle: '',
  source: '',
  region: '',
  leadType: '',
  productInterest: '',
  cnpj: '',
  campaignId: null,
})

const campaigns = ref<Campaign[]>([])
const campaignForm = reactive({
  name: '',
  channel: '',
  utm: '',
  isActive: true,
})

const dashboard = ref<DashboardOverview | null>(null)

const users = ref<User[]>([])
const userForm = reactive<CreateUserPayload>({
  username: '',
  password: '',
  role: 'marketing',
})

const hasSelectedLead = computed(() => selectedLead.value !== null)

function setError(error: unknown): void {
  errorMessage.value = error instanceof Error ? error.message : 'Unexpected error'
}

function clearError(): void {
  errorMessage.value = ''
}

function resetLeadForm(): void {
  createLeadForm.name = ''
  createLeadForm.email = ''
  createLeadForm.phone = ''
  createLeadForm.company = ''
  createLeadForm.jobTitle = ''
  createLeadForm.source = ''
  createLeadForm.region = ''
  createLeadForm.leadType = ''
  createLeadForm.productInterest = ''
  createLeadForm.cnpj = ''
  createLeadForm.campaignId = null
}

async function ensureAuthenticated(): Promise<void> {
  authenticated.value = getAccessToken().length > 0
  if (authenticated.value) {
    await loadEverything()
  }
}

async function handleLogin(): Promise<void> {
  clearError()
  loading.value = true
  try {
    await login(loginForm)
    authenticated.value = true
    await loadEverything()
  } catch (error) {
    setError(error)
  } finally {
    loading.value = false
  }
}

async function handleLogout(): Promise<void> {
  clearError()
  await logout()
  authenticated.value = false
  selectedLead.value = null
  selectedLeadId.value = ''
}

async function loadLeadsData(): Promise<void> {
  const page = await listLeads(leadFilters)
  leadPage.value = page
  if (!selectedLeadId.value && page.items.length > 0) {
    await loadLead(page.items[0].id)
  }
}

async function loadLead(id: string): Promise<void> {
  if (!id) {
    return
  }

  const lead = await getLeadById(id)
  const history = await listLeadHistory(id, 1, 20)
  selectedLead.value = lead
  selectedLeadId.value = id
  selectedStatus.value = lead.status
  leadHistory.value = history.items
}

async function submitCreateLead(): Promise<void> {
  clearError()
  loading.value = true
  try {
    const created = await createLead({ ...createLeadForm })
    resetLeadForm()
    await loadLeadsData()
    await loadLead(created.id)
    await loadDashboardData()
  } catch (error) {
    setError(error)
  } finally {
    loading.value = false
  }
}

async function submitStatusUpdate(): Promise<void> {
  if (!selectedLead.value) {
    return
  }

  clearError()
  loading.value = true
  try {
    selectedLead.value = await updateLeadStatus(selectedLead.value.id, { status: selectedStatus.value })
    await loadLeadsData()
    await loadLead(selectedLead.value.id)
    await loadDashboardData()
  } catch (error) {
    setError(error)
  } finally {
    loading.value = false
  }
}

async function submitRecalculateScore(): Promise<void> {
  if (!selectedLead.value) {
    return
  }

  clearError()
  loading.value = true
  try {
    selectedLead.value = await recalculateLeadScore(selectedLead.value.id)
    await loadLeadsData()
    await loadLead(selectedLead.value.id)
    await loadDashboardData()
  } catch (error) {
    setError(error)
  } finally {
    loading.value = false
  }
}

async function submitMergeLead(): Promise<void> {
  if (!selectedLead.value || !mergeSourceLeadId.value) {
    return
  }

  clearError()
  loading.value = true
  try {
    selectedLead.value = await mergeLead(selectedLead.value.id, {
      sourceLeadId: mergeSourceLeadId.value,
      precedence: 'Target',
    })
    mergeSourceLeadId.value = ''
    await loadLeadsData()
    await loadLead(selectedLead.value.id)
    await loadDashboardData()
  } catch (error) {
    setError(error)
  } finally {
    loading.value = false
  }
}

async function submitSyncCrm(): Promise<void> {
  if (!selectedLead.value) {
    return
  }

  clearError()
  loading.value = true
  try {
    selectedLead.value = await syncLeadToCrm(selectedLead.value.id)
  } catch (error) {
    setError(error)
  } finally {
    loading.value = false
  }
}

async function loadCampaignsData(): Promise<void> {
  campaigns.value = await listCampaigns()
}

async function submitCreateCampaign(): Promise<void> {
  clearError()
  loading.value = true
  try {
    await createCampaign({ ...campaignForm })
    campaignForm.name = ''
    campaignForm.channel = ''
    campaignForm.utm = ''
    campaignForm.isActive = true
    await loadCampaignsData()
  } catch (error) {
    setError(error)
  } finally {
    loading.value = false
  }
}

async function submitDeleteCampaign(id: string): Promise<void> {
  clearError()
  loading.value = true
  try {
    await deleteCampaign(id)
    await loadCampaignsData()
  } catch (error) {
    setError(error)
  } finally {
    loading.value = false
  }
}

async function loadDashboardData(): Promise<void> {
  dashboard.value = await getDashboardOverview()
}

async function loadUsersData(): Promise<void> {
  users.value = await listUsers()
}

async function submitCreateUser(): Promise<void> {
  clearError()
  loading.value = true
  try {
    await createUser({ ...userForm })
    userForm.username = ''
    userForm.password = ''
    userForm.role = 'marketing'
    await loadUsersData()
  } catch (error) {
    setError(error)
  } finally {
    loading.value = false
  }
}

async function submitDeleteUser(id: string): Promise<void> {
  clearError()
  loading.value = true
  try {
    await deleteUser(id)
    await loadUsersData()
  } catch (error) {
    setError(error)
  } finally {
    loading.value = false
  }
}

async function loadEverything(): Promise<void> {
  clearError()
  loading.value = true
  try {
    await Promise.all([
      loadLeadsData(),
      loadCampaignsData(),
      loadDashboardData(),
      loadUsersData(),
    ])
  } catch (error) {
    setError(error)
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  await ensureAuthenticated()
})
</script>

<template>
  <main class="leads-module">
    <header class="module-header">
      <div>
        <h1>Lead Management Platform</h1>
        <p>Auth, leads, campanhas, dashboard e administração em fluxo unificado.</p>
      </div>
      <button v-if="authenticated" :disabled="loading" @click="handleLogout">Logout</button>
    </header>

    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>

    <section v-if="!authenticated" class="card">
      <h2>Login</h2>
      <form class="grid" @submit.prevent="handleLogin">
        <label>
          Username
          <input v-model="loginForm.username" required />
        </label>
        <label>
          Password
          <input v-model="loginForm.password" required type="password" />
        </label>
        <button :disabled="loading" type="submit">{{ loading ? 'Entrando...' : 'Entrar' }}</button>
      </form>
    </section>

    <template v-else>
      <nav class="tabs">
        <button :class="{ active: activeTab === 'leads' }" @click="activeTab = 'leads'">Leads</button>
        <button :class="{ active: activeTab === 'campaigns' }" @click="activeTab = 'campaigns'">Campanhas</button>
        <button :class="{ active: activeTab === 'dashboard' }" @click="activeTab = 'dashboard'">Dashboard</button>
        <button :class="{ active: activeTab === 'users' }" @click="activeTab = 'users'">Usuários</button>
      </nav>

      <section v-if="activeTab === 'leads'" class="stack">
        <article class="card">
          <div class="row">
            <h2>Filtros e paginação</h2>
            <button :disabled="loading" @click="loadLeadsData">Atualizar</button>
          </div>
          <form class="grid compact" @submit.prevent="loadLeadsData">
            <label>
              Busca
              <input v-model="leadFilters.search" />
            </label>
            <label>
              Região
              <input v-model="leadFilters.region" />
            </label>
            <label>
              Status
              <select v-model="leadFilters.status">
                <option :value="undefined">Todos</option>
                <option v-for="status in statuses" :key="status" :value="status">{{ status }}</option>
              </select>
            </label>
            <label>
              Página
              <input v-model.number="leadFilters.page" min="1" type="number" />
            </label>
            <label>
              Tamanho
              <input v-model.number="leadFilters.pageSize" min="1" max="100" type="number" />
            </label>
            <button :disabled="loading" type="submit">Aplicar</button>
          </form>
          <p class="muted">
            Exibindo {{ leadPage.items.length }} de {{ leadPage.totalItems }} leads
            (página {{ leadPage.page }} de {{ leadPage.totalPages || 1 }})
          </p>
          <ul class="leads-list">
            <li v-for="lead in leadPage.items" :key="lead.id">
              <button class="lead-item" @click="loadLead(lead.id)">
                <strong>{{ lead.name }}</strong>
                <span>{{ lead.company }} · {{ lead.region }}</span>
                <span>{{ lead.status }} · Score {{ lead.score }}</span>
              </button>
            </li>
          </ul>
        </article>

        <article class="card">
          <h2>Criar lead</h2>
          <form class="grid" @submit.prevent="submitCreateLead">
            <label>Name<input v-model="createLeadForm.name" required /></label>
            <label>Email<input v-model="createLeadForm.email" required type="email" /></label>
            <label>Phone<input v-model="createLeadForm.phone" required /></label>
            <label>Company<input v-model="createLeadForm.company" required /></label>
            <label>Job title<input v-model="createLeadForm.jobTitle" required /></label>
            <label>Source<input v-model="createLeadForm.source" required /></label>
            <label>Region<input v-model="createLeadForm.region" required /></label>
            <label>Lead type<input v-model="createLeadForm.leadType" /></label>
            <label>Product interest<input v-model="createLeadForm.productInterest" /></label>
            <label>CNPJ<input v-model="createLeadForm.cnpj" /></label>
            <label>
              Campaign
              <select v-model="createLeadForm.campaignId">
                <option :value="null">Sem campanha</option>
                <option v-for="campaign in campaigns" :key="campaign.id" :value="campaign.id">
                  {{ campaign.name }}
                </option>
              </select>
            </label>
            <button :disabled="loading" type="submit">Salvar lead</button>
          </form>
        </article>

        <article class="card">
          <h2>Detalhes e ações</h2>
          <div v-if="hasSelectedLead" class="detail">
            <h3>{{ selectedLead?.name }}</h3>
            <p>{{ selectedLead?.email }} · {{ selectedLead?.phone }}</p>
            <p>{{ selectedLead?.company }} · {{ selectedLead?.jobTitle }}</p>
            <p>Status: {{ selectedLead?.status }} · Score: {{ selectedLead?.score }}</p>
            <div class="actions">
              <label>
                Novo status
                <select v-model="selectedStatus">
                  <option v-for="status in statuses" :key="status" :value="status">{{ status }}</option>
                </select>
              </label>
              <button :disabled="loading" @click="submitStatusUpdate">Atualizar status</button>
              <button :disabled="loading" @click="submitRecalculateScore">Recalcular score</button>
            </div>
            <div class="actions">
              <label>
                Merge source id
                <input v-model="mergeSourceLeadId" placeholder="GUID do lead origem" />
              </label>
              <button :disabled="loading" @click="submitMergeLead">Mesclar</button>
              <button :disabled="loading" @click="submitSyncCrm">Sincronizar CRM</button>
            </div>
            <h4>Histórico</h4>
            <ul class="history-list">
              <li v-for="entry in leadHistory" :key="entry.id">
                <span>{{ entry.eventType }} · {{ entry.fieldName }}</span>
                <strong>{{ entry.oldValue || '-' }} → {{ entry.newValue || '-' }}</strong>
              </li>
            </ul>
          </div>
          <p v-else>Selecione um lead para visualizar detalhes.</p>
        </article>
      </section>

      <section v-if="activeTab === 'campaigns'" class="stack">
        <article class="card">
          <h2>Criar campanha</h2>
          <form class="grid compact" @submit.prevent="submitCreateCampaign">
            <label>Nome<input v-model="campaignForm.name" required /></label>
            <label>Canal<input v-model="campaignForm.channel" required /></label>
            <label>UTM<input v-model="campaignForm.utm" /></label>
            <label>
              Ativa
              <select v-model="campaignForm.isActive">
                <option :value="true">Sim</option>
                <option :value="false">Não</option>
              </select>
            </label>
            <button :disabled="loading" type="submit">Salvar campanha</button>
          </form>
        </article>
        <article class="card">
          <h2>Campanhas</h2>
          <ul class="history-list">
            <li v-for="campaign in campaigns" :key="campaign.id">
              <span>{{ campaign.name }} · {{ campaign.channel }} · {{ campaign.utm || '-' }}</span>
              <button :disabled="loading" @click="submitDeleteCampaign(campaign.id)">Excluir</button>
            </li>
          </ul>
        </article>
      </section>

      <section v-if="activeTab === 'dashboard'" class="stack">
        <article class="card" v-if="dashboard">
          <h2>Visão geral</h2>
          <div class="grid compact">
            <p>Total: <strong>{{ dashboard.totalLeads }}</strong></p>
            <p>Novos: <strong>{{ dashboard.newLeads }}</strong></p>
            <p>Em atendimento: <strong>{{ dashboard.inServiceLeads }}</strong></p>
            <p>Qualificados: <strong>{{ dashboard.qualifiedLeads }}</strong></p>
            <p>Convertidos: <strong>{{ dashboard.convertedLeads }}</strong></p>
            <p>Perdidos: <strong>{{ dashboard.lostLeads }}</strong></p>
            <p>Score médio: <strong>{{ dashboard.averageScore }}</strong></p>
            <p>Conversão: <strong>{{ dashboard.conversionRate }}%</strong></p>
          </div>
          <h3>Por temperatura</h3>
          <ul class="history-list">
            <li v-for="entry in dashboard.byTemperature" :key="entry.name">{{ entry.name }}: {{ entry.count }}</li>
          </ul>
          <h3>Por origem</h3>
          <ul class="history-list">
            <li v-for="entry in dashboard.bySource" :key="entry.name">{{ entry.name }}: {{ entry.count }}</li>
          </ul>
        </article>
      </section>

      <section v-if="activeTab === 'users'" class="stack">
        <article class="card">
          <h2>Criar usuário</h2>
          <form class="grid compact" @submit.prevent="submitCreateUser">
            <label>Username<input v-model="userForm.username" required /></label>
            <label>Password<input v-model="userForm.password" required type="password" /></label>
            <label>
              Role
              <select v-model="userForm.role">
                <option v-for="role in roles" :key="role" :value="role">{{ role }}</option>
              </select>
            </label>
            <button :disabled="loading" type="submit">Salvar usuário</button>
          </form>
        </article>
        <article class="card">
          <h2>Usuários</h2>
          <ul class="history-list">
            <li v-for="user in users" :key="user.id">
              <span>{{ user.username }} · {{ user.role }}</span>
              <button :disabled="loading" @click="submitDeleteUser(user.id)">Excluir</button>
            </li>
          </ul>
        </article>
      </section>
    </template>
  </main>
</template>
