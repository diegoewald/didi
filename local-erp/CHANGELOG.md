# Changelog

Todas as mudanças relevantes serão registradas aqui.

O projeto segue versionamento semântico.

## [Unreleased]

### Added

- fundação .NET 10/WPF, SQLite/EF Core, WAL e tooling de migrations;
- autenticação local, sessão, RBAC deny-by-default e bootstrap atômico;
- throttling/lockout, mitigação de timing e `SecurityAuditEvents`;
- administração de usuários, roles/permissões e auditoria administrativa atômica;
- troca de senha pelo próprio usuário com senha atual e compare-and-set transacional;
- catálogo WPF de Empresas com listagem, criação, renomeação e status;
- entidade `CompanyBranch` com `CompanyId`, nome e status;
- `CompanyBranchService`, `ICompanyBranchRepository` e `EfCompanyBranchRepository`;
- migration `20261018105000_AddCompanyBranches` com FK para `Companies` e índice por `CompanyId`;
- testes de domínio/aplicação/persistência para Filiais, incluindo rejeição de FK órfã;
- janela WPF `CompanyBranchesWindow` acessível por `Gerenciar` em cada empresa, com listagem, criação, rename e status.

### Changed

- Filiais reutilizam `companies.read` para consulta e `companies.manage` para mutações;
- FK de filial usa `ON DELETE RESTRICT`, evitando exclusão silenciosa de filiais por uma futura exclusão de empresa;
- status de empresa e filial permanecem independentes; não existe cascata implícita;
- dados fiscais, endereço, códigos internos e regra de matriz permanecem fora do modelo até requisito explícito;
- shell `Cadastros` agora possui empresa + filial como primeiro cadastro hierárquico completo.

### Validation

- Windows Server 2025;
- .NET SDK 10.0.402 / runtime 10.0.13;
- Release build: 0 warnings / 0 errors;
- benchmark tool: 0 warnings / 0 errors;
- suíte automatizada: 138/138;
- Domain 22/22, Application 69/69, Persistence 41/41, Infrastructure 6/6;
- EF Core 10.0.13: sem mudanças pendentes no modelo;
- 7 migrations: apply -> rollback 0 -> reapply passando;
- CI #104 validou backend, FK, snapshot e migration de Filiais;
- CI #105 validou o XAML/code-behind de Filiais sobre o mesmo baseline;
- smoke visual/interativo em Windows real permanece obrigatório antes de release.

## [0.1.0] - 2026-10-15

- fundação da solução em .NET 10 com arquitetura em camadas;
- WPF, EF Core 10 + SQLite, Serilog e MSTest;
- documentação inicial, ADRs e backlog central.

## Marcos de segurança — 2026-10-16 a 2026-10-17

- autenticação local utilizável, RBAC e bootstrap seguro;
- hardening de autenticação e auditoria persistida;
- administração de usuários Partes A–F;
- administração completa de roles/permissões com salvaguarda da última capacidade efetiva;
- troca própria de senha com auditoria atômica e proteção contra corrida de credencial;
- baseline anterior aos cadastros-base: 127 testes e 6 migrations.

## Marco de cadastro de empresas — 2026-10-17

- `Cadastros` ganhou tela operacional de Empresas;
- listagem exige `companies.read`; mutações exigem `companies.manage`;
- criação, renomeação e ativação/desativação disponíveis no WPF;
- baseline subiu para 129 testes, sem migration nova.

## Marco de filiais — 2026-10-18

- `CompanyBranch` vinculada obrigatoriamente a `Company` por FK `RESTRICT`;
- serviço e repositório suportam lista por empresa, criação, rename e status;
- migration `20261018105000_AddCompanyBranches` validada em apply/rollback/reapply;
- janela `CompanyBranchesWindow` integrada ao catálogo de Empresas;
- baseline subiu para 138 testes e 7 migrations.
