# LeadManager Frontend

SPA em Vue 3 para operar o backend de gestão de leads.

## Fluxos implementados

- Login com `accessToken + refreshToken`.
- Gestão de leads com filtros, paginação, detalhe, merge, histórico e sync CRM.
- CRUD de campanhas.
- Dashboard com métricas de conversão e score.
- Administração de usuários/roles.
- Tratamento de erro padronizado para `ProblemDetails`.

## Scripts

```bash
pnpm install --frozen-lockfile
pnpm dev
pnpm test
pnpm build
```
