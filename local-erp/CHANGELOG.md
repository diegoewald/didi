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
- entidade `CompanyBranch`, serviço/repositório e migration `20261018105000_AddCompanyBranches`;
- janela WPF `CompanyBranchesWindow` acessível por `Gerenciar` em cada empresa;
- entidade `Supplier` com nome/status, `SupplierService`, `ISupplierRepository` e `EfSupplierRepository`;
- permissões `suppliers.read` e `suppliers.manage` no `PermissionCatalog`;
- migration `20261018142500_AddSuppliers`;
- catálogo WPF de Fornecedores com listagem de ativos/inativos, criação, renomeação e status;
- tratamento global de exceções do shell para `DispatcherUnhandledException`, `AppDomain.UnhandledException` e `TaskScheduler.UnobservedTaskException`.

### Changed

- Filiais reutilizam `companies.read` para consulta e `companies.manage` para mutações;
- FK de filial usa `ON DELETE RESTRICT`, evitando exclusão silenciosa de filiais por uma futura exclusão de empresa;
- status de empresa e filial permanecem independentes; não existe cascata implícita;
- Fornecedores usam autorização própria `suppliers.read/manage` na Application;
- dados fiscais/endereço de filial e campos fiscais, bancários ou comerciais de fornecedor permanecem fora do modelo até requisito explícito;
- falha não tratada no dispatcher WPF agora é logada como fatal, mostra mensagem genérica e encerra a aplicação para evitar continuidade em estado incerto;
- exceções não observadas de task são registradas e marcadas como observadas; exceções não tratadas do `AppDomain` recebem logging final.

### Validation

- Windows Server 2025;
- .NET SDK 10.0.402 / runtime 10.0.13;
- Release build: 0 warnings / 0 errors;
- benchmark tool: 0 warnings / 0 errors;
- suíte automatizada: 148/148;
- Domain 26/26, Application 74/74, Persistence 42/42, Infrastructure 6/6;
- EF Core 10.0.13: sem mudanças pendentes no modelo;
- 8 migrations: apply -> rollback 0 -> reapply passando;
- CI #108 validou o slice completo de Fornecedores;
- CI #109 recompilou o tratamento global de exceções e preservou todo o baseline;
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

## Marco de fornecedores — 2026-10-18

- `Supplier` adicionado com identidade, nome e status;
- `suppliers.read` e `suppliers.manage` adicionados ao catálogo de permissões;
- serviço/repositório e migration `20261018142500_AddSuppliers` integrados;
- catálogo WPF de Fornecedores operacional;
- baseline subiu para 148 testes e 8 migrations;
- CI #108 validou build, testes, snapshot e ciclo completo de migrations.

## Marco de robustez do shell — 2026-10-18

- tratamento central de exceções não tratadas passou a existir além do startup;
- falhas fatais do dispatcher são registradas e provocam encerramento controlado com mensagem genérica;
- `AppDomain.UnhandledException` e `TaskScheduler.UnobservedTaskException` recebem logging final;
- CI #109 preservou 148/148 testes e 8 migrations.
