# Roadmap

Legenda: `[ ]` pendente, `[-]` em andamento, `[x]` concluído.

## Fase 0 — Pesquisa e arquitetura

- [x] stack .NET 10 LTS + WPF;
- [x] SQLite single-PC e PostgreSQL futuro;
- [x] arquitetura inicial e ADRs.

## Fase 1 — Fundação técnica

- [x] solução em camadas, logging e diretórios locais;
- [x] restore/build/test Windows sem warnings/erros;
- [x] CI Windows permanente.

## Fase 2 — Persistência

- [x] `Company`, `DbContext` e migrations;
- [x] WAL, foreign keys e migration smoke;
- [x] política de valores monetários e seed de desenvolvimento.

## Fase 3 — Autenticação e autorização

- [x] usuários, hashing, autenticação e sessão;
- [x] RBAC e autorização deny-by-default;
- [x] bootstrap/login/logout;
- [x] lockout/throttling, mitigação de timing e auditoria;
- [x] política de senha central e benchmark Windows CI;
- [ ] benchmark em hardware Windows representativo do ambiente final.

## Fase 4 — Shell da aplicação

- [-] navegação real por módulos;
- [ ] tratamento global de erro além do startup;
- [ ] atalhos e foco/teclado;
- [-] estados loading/empty/error já existem em Administração, Empresas e Filiais, ainda sem componente comum;
- [ ] componentes reutilizáveis de tabela/filtro quando repetição justificar;
- [ ] smoke visual/interativo em Windows real.

## Fase 5 — Administração de usuários e roles

- [x] administração de usuários Partes A–F;
- [x] roles/permissões completas e salvaguarda da última capacidade efetiva;
- [x] auditoria atômica das mutações administrativas;
- [x] troca própria de senha com senha atual e compare-and-set transacional.

## Fase 6 — Cadastros-base

- [x] empresa: listagem, criação, renomeação e status;
- [x] filial: `CompanyBranch` vinculada obrigatoriamente à empresa;
- [x] filial: persistência, migration, autorização e UI operacional;
- [-] fornecedores: próxima frente funcional;
- [ ] produtos/materiais;
- [ ] unidades/categorias;
- [ ] centros de custo.

## Fase 7 — Primeiro módulo operacional

- [ ] compras: fluxo mínimo a definir com regras de negócio reais.

## Fases posteriores

- [ ] relatórios;
- [ ] consulta/retenção operacional da trilha de auditoria;
- [ ] backup/restauração;
- [ ] atualização com migration bundle e recuperação segura;
- [ ] instalador;
- [ ] performance/QA;
- [ ] preparação multiusuário;
- [ ] release 1.0.

## Marco de empresa/filial — 2026-10-18

`Company` e `CompanyBranch` formam o primeiro cadastro hierárquico operacional. `CompanyBranch` possui somente identidade, `CompanyId`, nome e status. A FK `CompanyBranches.CompanyId -> Companies.Id` é obrigatória e usa `RESTRICT`; não há filial órfã nem cascata de exclusão implícita.

`CompanyBranchService` exige `companies.read` para listagem e `companies.manage` para criação, renomeação e status. A criação/listagem valida que a empresa existe. Desativar empresa ou filial preserva os registros e não há cascata automática de status.

A tela `Empresas` ganhou ação `Gerenciar` para abrir `CompanyBranchesWindow`, onde filiais ativas/inativas podem ser listadas, criadas, renomeadas, ativadas e desativadas. Dados fiscais, endereço, códigos internos e conceito de matriz não foram presumidos.

A migration `20261018105000_AddCompanyBranches` adiciona tabela, FK e índice. CI #104 validou domínio/persistência/migration; CI #105 validou o WPF sobre o mesmo baseline. A suíte chegou a 138/138 testes (Domain 22, Application 69, Persistence 41, Infrastructure 6), com 7 migrations e snapshot EF consistente.

Próxima prioridade funcional: **Fornecedores**.
