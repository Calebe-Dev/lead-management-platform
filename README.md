# Lead Management Platform

Plataforma full-stack para gestão de leads, com backend em .NET (Clean Architecture), frontend em Vue 3 + TypeScript, persistência em PostgreSQL e cache em Redis.

## Arquitetura implementada

- **Backend API**: `LeadManager.API` (Minimal APIs, JWT bearer auth, middleware de tratamento de erro)
- **Camada de aplicação**: `LeadManager.Application` (use cases e contratos)
- **Domínio**: `LeadManager.Domain` (entidades, enums e regras)
- **Infraestrutura**: `LeadManager.Infrastructure` (EF Core + Npgsql, Redis cache, distribuição round-robin)
- **Frontend**: `LeadManager` (Vue + Vite + Vitest)
- **Testes**:
  - `LeadManager.Tests.Unit`
  - `LeadManager.Tests.Integration`

## Funcionalidades principais

- Emissão de token JWT por usuário/role
- CRUD parcial de leads (criação, listagem, detalhe)
- Atualização de status com autorização por role
- Recalcular score do lead
- Histórico por lead
- Filtros e paginação na listagem
- Cache distribuído de listagens via Redis

## Segurança

- Autenticação: `Bearer JWT`
- Autorização por role:
  - `admin`, `marketing`, `vendas` em `/api/leads` (acesso geral)
  - `admin` e `vendas` para atualizar status e recalcular score
- Validação estrita de configuração JWT (issuer/audience/key/expiração)
- Middleware de erros com respostas HTTP explícitas (400/409)

## Endpoints da API

Base: `/api`

- `POST /auth/token` – gera token JWT
- `POST /leads` – cria lead
- `GET /leads` – lista leads (com filtros e paginação)
- `GET /leads/{id}` – detalhe de lead
- `GET /leads/{id}/history` – histórico do lead
- `PATCH /leads/{id}/status` – altera status (admin/vendas)
- `POST /leads/{id}/score` – recalcula score (admin/vendas)

### Filtros de `GET /api/leads`

`status`, `temperature`, `region`, `leadType`, `productInterest`, `assignedTo`, `search`, `minScore`, `maxScore`, `page`, `pageSize`.

## Como rodar localmente (sem Docker)

### Pré-requisitos

- .NET SDK 10
- Node 22 + pnpm 10
- PostgreSQL
- Redis

### 1) Backend

```bash
dotnet restore lead-management-platform.sln
dotnet run --project LeadManager.API
```

API local: `http://localhost:5039`

### 2) Frontend

```bash
cd LeadManager
pnpm install --frozen-lockfile
pnpm dev
```

Frontend local: `http://localhost:5173`

> O Vite está configurado com proxy `/api -> http://localhost:5039`.

### 3) Token para chamadas autenticadas

Exemplo para obter token:

```bash
curl -s http://localhost:5039/api/auth/token \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123!"}'
```

Para usar no frontend atual, salve o token no navegador:

```js
localStorage.setItem('lead_manager_token', '<JWT_AQUI>')
```

## Como rodar com Docker Compose

### Serviços

- `postgres` (5432)
- `redis` (6379)
- `api` (8080)
- `frontend` (5173)

### Subir stack

```bash
docker compose up --build
```

Acessos:

- Frontend: `http://localhost:5173`
- API: `http://localhost:8080`

O frontend em container faz proxy de `/api` para o serviço `api` interno (`http://api:8080`).

## CI/CD (GitHub Actions)

Workflow principal: `.github/workflows/ci.yml`

Inclui:

- restore/build backend e projeto de integração
- testes unitários (`LeadManager.Tests.Unit`)
- testes de integração (`LeadManager.Tests.Integration`)
- testes + build frontend (`pnpm test`, `pnpm build`)
- quality gate de containers (`docker compose config` + build de imagens `api` e `frontend`)

Workflow auxiliar: `.github/workflows/phase-gates.yml` (validação manual por fase).

## Testes e validação

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
