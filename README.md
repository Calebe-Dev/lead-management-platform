# Lead Management Platform

Base de desenvolvimento do **Lead Management Platform** com foco em:

- Clean Architecture
- Clean Code
- Testes unitarios por componente
- Fluxo incremental de entrega (commit a commit)

## Estrutura inicial do repositorio

- `LeadManager.API/` API .NET
- `LeadManager/` Frontend Vue + TypeScript
- `.github/workflows/` pipelines CI/CD
- `workflow-templates/` fluxos de desenvolvimento por fase
- `role-definitions/` papeis de agentes
- `skill-definitions/` habilidades reutilizaveis dos agentes
- `workflow-templates/agent-stack.yaml` stack de agentes (orquestrador e especialistas)

## Diretrizes adotadas

- Seguir roteiro em `.references/roteiro.md`
- Seguir objetivos em `.references/objetivs.md`
- Cada alteracao relevante deve gerar commit
- `push` deve ser tentado apos cada commit

## Fases (resumo)

1. Setup e fundamentos
2. Backend MVP
3. Regras de negocio
4. Testes unitarios
5. Refatoracao para Clean Architecture
6. Persistencia real
7. Frontend funcional
8. Integracao e seguranca
9. Infra e CI/CD

## Observacao importante

As pastas de referencia e runtime de agentes de IA permanecem fora de versionamento via `.gitignore`.
