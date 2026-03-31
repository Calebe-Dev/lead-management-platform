<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import {
  createLead,
  getLeadById,
  listLeads,
  recalculateLeadScore,
  updateLeadStatus,
} from '../services/leadsService'
import type { CreateLeadPayload, Lead, LeadStatus } from '../types'

const statuses: LeadStatus[] = ['New', 'InService', 'Qualified', 'Converted', 'Lost']

const leads = ref<Lead[]>([])
const selectedLeadId = ref<string>('')
const selectedLead = ref<Lead | null>(null)

const loading = ref(false)
const creating = ref(false)
const selecting = ref(false)
const updatingStatus = ref(false)
const recalculatingScore = ref(false)
const errorMessage = ref('')

const createForm = reactive<CreateLeadPayload>({
  name: '',
  email: '',
  phone: '',
  company: '',
  jobTitle: '',
  source: '',
})

const selectedStatus = ref<LeadStatus>('New')
const manualLeadId = ref('')

const hasSelectedLead = computed(() => selectedLead.value !== null)

function setError(error: unknown): void {
  errorMessage.value = error instanceof Error ? error.message : 'Unexpected error'
}

function clearForm(): void {
  createForm.name = ''
  createForm.email = ''
  createForm.phone = ''
  createForm.company = ''
  createForm.jobTitle = ''
  createForm.source = ''
}

async function loadLeads(): Promise<void> {
  loading.value = true
  errorMessage.value = ''
  try {
    leads.value = await listLeads()
    if (!selectedLeadId.value && leads.value.length > 0) {
      await loadLeadDetails(leads.value[0].id)
    }
  } catch (error) {
    setError(error)
  } finally {
    loading.value = false
  }
}

async function loadLeadDetails(id: string): Promise<void> {
  if (!id) {
    return
  }

  selecting.value = true
  errorMessage.value = ''
  try {
    const lead = await getLeadById(id)
    selectedLead.value = lead
    selectedLeadId.value = id
    selectedStatus.value = lead.status
    manualLeadId.value = id
  } catch (error) {
    setError(error)
  } finally {
    selecting.value = false
  }
}

async function submitCreateLead(): Promise<void> {
  creating.value = true
  errorMessage.value = ''

  try {
    const newLead = await createLead({ ...createForm })
    clearForm()
    await loadLeads()
    await loadLeadDetails(newLead.id)
  } catch (error) {
    setError(error)
  } finally {
    creating.value = false
  }
}

async function submitStatusUpdate(): Promise<void> {
  if (!selectedLead.value) {
    return
  }

  updatingStatus.value = true
  errorMessage.value = ''

  try {
    const updated = await updateLeadStatus(selectedLead.value.id, { status: selectedStatus.value })
    selectedLead.value = updated
    await loadLeads()
  } catch (error) {
    setError(error)
  } finally {
    updatingStatus.value = false
  }
}

async function triggerScoreRecalculation(): Promise<void> {
  if (!selectedLead.value) {
    return
  }

  recalculatingScore.value = true
  errorMessage.value = ''

  try {
    const updated = await recalculateLeadScore(selectedLead.value.id)
    selectedLead.value = updated
    await loadLeads()
  } catch (error) {
    setError(error)
  } finally {
    recalculatingScore.value = false
  }
}

onMounted(async () => {
  await loadLeads()
})
</script>

<template>
  <main class="leads-module">
    <header>
      <h1>Lead Management</h1>
      <p>Official lead flows integrated with current API endpoints.</p>
    </header>

    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>

    <section class="card">
      <h2>Create lead</h2>
      <form class="grid" @submit.prevent="submitCreateLead">
        <label>
          Name
          <input v-model="createForm.name" required />
        </label>
        <label>
          Email
          <input v-model="createForm.email" required type="email" />
        </label>
        <label>
          Phone
          <input v-model="createForm.phone" required />
        </label>
        <label>
          Company
          <input v-model="createForm.company" required />
        </label>
        <label>
          Job title
          <input v-model="createForm.jobTitle" required />
        </label>
        <label>
          Source
          <input v-model="createForm.source" required />
        </label>
        <button :disabled="creating" type="submit">{{ creating ? 'Creating...' : 'Create lead' }}</button>
      </form>
    </section>

    <section class="split">
      <article class="card">
        <div class="row">
          <h2>List leads</h2>
          <button :disabled="loading" @click="loadLeads">{{ loading ? 'Refreshing...' : 'Refresh' }}</button>
        </div>
        <ul class="leads-list">
          <li v-for="lead in leads" :key="lead.id">
            <button class="lead-item" @click="loadLeadDetails(lead.id)">
              <strong>{{ lead.name }}</strong>
              <span>{{ lead.company }}</span>
              <span>{{ lead.status }} · Score {{ lead.score }}</span>
            </button>
          </li>
        </ul>
      </article>

      <article class="card">
        <h2>Lead detail by id</h2>
        <form class="row" @submit.prevent="loadLeadDetails(manualLeadId)">
          <input v-model="manualLeadId" placeholder="Lead ID (GUID)" required />
          <button :disabled="selecting" type="submit">{{ selecting ? 'Loading...' : 'Load detail' }}</button>
        </form>

        <div v-if="hasSelectedLead" class="detail">
          <h3>{{ selectedLead?.name }}</h3>
          <p>{{ selectedLead?.email }} · {{ selectedLead?.phone }}</p>
          <p>{{ selectedLead?.company }} · {{ selectedLead?.jobTitle }}</p>
          <p>Source: {{ selectedLead?.source }}</p>
          <p>Status: {{ selectedLead?.status }}</p>
          <p>Score: {{ selectedLead?.score }} ({{ selectedLead?.temperature }})</p>

          <div class="actions">
            <label>
              Status update
              <select v-model="selectedStatus">
                <option v-for="status in statuses" :key="status" :value="status">{{ status }}</option>
              </select>
            </label>
            <button :disabled="updatingStatus" @click="submitStatusUpdate">
              {{ updatingStatus ? 'Updating...' : 'Update status' }}
            </button>
          </div>

          <button :disabled="recalculatingScore" @click="triggerScoreRecalculation">
            {{ recalculatingScore ? 'Recalculating...' : 'Recalculate score' }}
          </button>
        </div>
        <p v-else>Select a lead to view details.</p>
      </article>
    </section>
  </main>
</template>
