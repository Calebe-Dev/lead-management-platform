# Lead Management Platform

Plataforma full-stack para qualificação, scoring, distribuição, auditoria e sincronização de leads.

## Stack

- Backend: `.NET 10` com arquitetura em camadas (`API`, `Application`, `Domain`, `Infrastructure`)
- Frontend: `Vue 3 + TypeScript + Vite`
- Persistência operacional: `PostgreSQL`
- Cache distribuído: `Redis`
- Auditoria e logs de IA/comportamento: `MongoDB`
- Processamento assíncrono: `Outbox Worker`

## Entregas principais deste ciclo

- Contrato frontend/backend corrigido (`GET /api/leads` paginado).
- Payload de criação de lead estendido com `region`, `leadType`, `productInterest`, `cnpj`, `campaignId`.
- Parsing padronizado de erros `ProblemDetails` no frontend.
- Auth avançada:
  - `POST /api/auth/token`
  - `POST /api/auth/refresh`
  - `POST /api/auth/logout`
- Gestão de usuários com RBAC (`/api/users`).
- CRUD de campanhas (`/api/campaigns`).
- Merge de leads (`POST /api/leads/{id}/merge`).
- Histórico paginado (`GET /api/leads/{id}/history?page=&pageSize=`).
- Dashboard operacional (`GET /api/dashboard/overview`).
- Integrações:
  - Sync CRM (`POST /api/integrations/crm/sync/{leadId}`)
  - Webhooks HubSpot/Salesforce/WhatsApp
- Outbox com retry/backoff e worker dedicado.
- Infra de execução com `docker-compose` completo (`api`, `worker`, `frontend`, `postgres`, `redis`, `mongo`).
- Manifestos Kubernetes completos em `k8s/` com `kustomization`.

## Estrutura

- `LeadManager.API`: endpoints e middleware HTTP.
- `LeadManager.Application`: use cases e contratos.
- `LeadManager.Domain`: regras de negócio e entidade `Lead`.
- `LeadManager.Infrastructure`: EF Core, Redis, Mongo, scoring IA, integrações e outbox.
- `LeadManager.Worker`: processamento assíncrono de mensagens de outbox.
- `LeadManager`: frontend Vue.

## Pré-requisitos locais

- .NET SDK 10.x
- Node.js 22.x
- pnpm 10.x
- Docker + Docker Compose
- Kubernetes CLI (`kubectl`) opcional para validar manifestos

## Bootstrap manual local

1. Suba dependências:

```bash
docker compose up -d postgres redis mongo
```

2. Backend API:

```bash
dotnet restore lead-management-platform.sln
dotnet run --project LeadManager.API
```

3. Worker:

```bash
dotnet run --project LeadManager.Worker
```

4. Frontend:

```bash
cd LeadManager
pnpm install --frozen-lockfile
pnpm dev
```

## Rodando stack completa em containers

```bash
docker compose up --build
```

Acessos padrão:

- Frontend: `http://localhost:5173`
- API: `http://localhost:8080`

## Testes

Backend:

```bash
dotnet build lead-management-platform.sln -c Release
dotnet test LeadManager.Tests.Unit/LeadManager.Tests.Unit.csproj -c Release
dotnet test LeadManager.Tests.Integration/LeadManager.Tests.Integration.csproj -c Release
```

Frontend:

```bash
cd LeadManager
pnpm test
pnpm build
```

## CI/CD

Workflow principal em `.github/workflows/ci.yml` com gates:

- build/test backend
- test/build frontend
- validação e build de containers (`api`, `worker`, `frontend`)
- render de manifestos Kubernetes via `kubectl kustomize k8s`

## Kubernetes

Manifestos em `k8s/` com:

- `postgres`, `redis`, `mongo`
- `lead-manager-api`, `lead-manager-worker`, `lead-manager-frontend`
- `ingress`, `HPA`, `configmap` e `secret` de exemplo

Render local:

```bash
kubectl kustomize k8s
```
