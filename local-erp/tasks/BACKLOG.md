# Backlog

## P0 — Pré-release / validação de ambiente

- [ ] executar smoke visual/interativo do WPF em Windows real;
- [ ] executar benchmark do password hashing em hardware Windows representativo do ambiente alvo.

## P1 — Segurança e administração

- [x] autenticação local, sessão, RBAC e autorização deny-by-default;
- [x] bootstrap seguro do primeiro administrador;
- [x] throttling/lockout e mitigação de timing;
- [x] trilha persistida de auditoria de autenticação;
- [x] administração de usuários Partes A–F;
- [x] administração completa de roles/permissões;
- [x] salvaguardas da última capacidade efetiva de `roles.manage`;
- [x] auditoria atômica das mutações administrativas expostas;
- [x] troca de senha pelo próprio usuário com senha atual, compare-and-set e auditoria atômica;
- [ ] tela/consulta administrativa da trilha de auditoria quando houver necessidade operacional;
- [ ] definir política de retenção/limpeza de `SecurityAuditEvents`;
- [ ] avaliar política para falhas de escrita de auditoria em caminhos de login que não alteram estado.

## P1 — Shell e robustez

- [x] tratamento global de exceções não tratadas no dispatcher WPF;
- [x] logging final de `AppDomain.UnhandledException` e `TaskScheduler.UnobservedTaskException`;
- [x] mensagem fatal genérica ao usuário sem stack trace e encerramento controlado do shell em falha de dispatcher;
- [ ] atalhos e foco/teclado do shell;
- [ ] consolidar componentes loading/empty/error somente quando a repetição justificar;
- [ ] smoke manual específico do fluxo fatal em build de diagnóstico, sem injetar falha em produção.

## P1 — Cadastros-base

- [x] empresa: domínio/persistência existentes e UI operacional em `Cadastros`;
- [x] empresa: listagem de ativos/inativos, criação, renomeação e status;
- [x] empresa: `companies.read` para consulta e `companies.manage` para mutações;
- [x] filial: entidade `CompanyBranch` com `CompanyId`, nome e status;
- [x] filial: FK obrigatória para `Company` com `ON DELETE RESTRICT` e índice por `CompanyId`;
- [x] filial: serviço/repositório com listagem por empresa, criação, renomeação e status;
- [x] filial: reutilizar `companies.read/manage` sem criar permissão redundante;
- [x] filial: janela WPF acessível pelo botão `Gerenciar` de cada empresa;
- [x] migration `20261018105000_AddCompanyBranches` validada em apply/rollback/reapply;
- [x] fornecedor: entidade `Supplier` com nome/status;
- [x] fornecedor: `suppliers.read` e `suppliers.manage` registrados no `PermissionCatalog`;
- [x] fornecedor: serviço/repositório com listagem, criação, renomeação e status;
- [x] fornecedor: catálogo WPF com ativos/inativos e mutações protegidas pela Application;
- [x] migration `20261018142500_AddSuppliers` validada em apply/rollback/reapply;
- [ ] fornecedor: não adicionar CNPJ/CPF, endereço, contatos, dados bancários ou condições comerciais sem requisito explícito;
- [x] material: entidade `Material` com identidade, nome e status;
- [x] material: `materials.read` e `materials.manage` registrados no `PermissionCatalog`;
- [x] material: serviço/repositório com listagem, criação, renomeação e status;
- [x] material: migration `20261018222000_AddMaterials` validada em apply/rollback/reapply;
- [x] material: domínio, autorização e persistência cobertos por testes;
- [x] material: catálogo WPF operacional em `Cadastros`, com ativos/inativos, criação, renomeação e status;
- [ ] material: não adicionar código/SKU, unidade, categoria, estoque, preço ou vínculo comercial sem requisito explícito;
- [x] produto: entidade `Product` separada de `Material`, com identidade, nome e status;
- [x] produto: `products.read` e `products.manage` registrados no `PermissionCatalog`;
- [x] produto: serviço/repositório com listagem, criação, renomeação e status;
- [x] produto: migration `20261019005000_AddProducts` validada em apply/rollback/reapply;
- [x] produto: domínio, autorização e persistência cobertos por testes;
- [x] produto: catálogo WPF operacional em `Cadastros`, com ativos/inativos, criação, renomeação e status;
- [ ] produto: não adicionar código/SKU, unidade, categoria, estoque, preço, tributação, composição/BOM ou vínculo comercial sem requisito explícito;
- [x] unidade de medida: entidade `MeasurementUnit` independente, com identidade, nome e status;
- [x] unidade de medida: `units.read` e `units.manage` registrados no `PermissionCatalog`;
- [x] unidade de medida: serviço/repositório com listagem, criação, renomeação e status;
- [x] unidade de medida: migration `20261019093000_AddMeasurementUnits` validada em apply/rollback/reapply;
- [x] unidade de medida: domínio, autorização e persistência cobertos por testes;
- [ ] unidade de medida: catálogo WPF operacional em `Cadastros`;
- [ ] unidade de medida: não adicionar sigla/símbolo, precisão, fatores de conversão ou vínculo com Produto/Material sem requisito explícito;
- [ ] categorias: modelar separadamente somente após definir semântica mínima do cadastro-base;
- [ ] centros de custo.

## P1 — Persistência

- [x] tooling local do `dotnet-ef`, WAL e foreign keys;
- [x] migrations iniciais e ciclo up/down no CI;
- [x] RBAC e auditoria com FKs reais;
- [x] transações SQLite imediatas nos fluxos concorrentes críticos;
- [x] `CompanyBranches` com integridade referencial para `Companies`;
- [x] `Suppliers` integrado ao snapshot EF e migration smoke;
- [x] `Materials` integrado ao snapshot EF e migration smoke;
- [x] `Products` integrado ao snapshot EF e migration smoke;
- [x] `MeasurementUnits` integrado ao snapshot EF e migration smoke;
- [ ] revisar o aviso conhecido do rollback de `AddAuthenticationThrottling` antes de release, sem reescrever histórico de migration de forma insegura.

## P2 — Engenharia

- [x] CI Windows permanente;
- [x] `has-pending-model-changes` no CI;
- [x] build da ferramenta de benchmark no CI;
- [ ] coverage quando a suíte tiver volume suficiente;
- [ ] verificação automatizada de dependências vulneráveis;
- [ ] script de package/release;
- [ ] teste automatizado de UI somente para fluxos cujo custo se justificar.

## Baseline atual — 2026-10-19

Empresa, filial, fornecedor, material e produto estão operacionais nos cadastros-base. O núcleo de unidade de medida também está implementado e validado, mas ainda não possui catálogo WPF.

`MeasurementUnit` é um cadastro global independente com somente identidade, nome e status. `MeasurementUnitService` exige `units.read` para consulta e `units.manage` para mutações. Não existe vínculo automático com Produto ou Material, nem sigla/símbolo, precisão ou conversão nesta etapa.

Instalações novas recebem `units.read/manage` pelo bootstrap; roles existentes não recebem grants novos automaticamente.

Baseline automatizado: 178/178 testes (Domain 38, Application 89, Persistence 45, Infrastructure 6), 11 migrations, build Release e benchmark tool sem warnings/erros, snapshot EF sem mudanças pendentes e apply -> rollback 0 -> reapply validado no CI #120.

Próxima tarefa funcional: **catálogo WPF de unidades de medida**. Categorias permanece uma frente separada. Smoke WPF e benchmark em hardware representativo continuam pendências de pré-release.
