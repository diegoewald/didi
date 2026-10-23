# Changelog

Todas as mudanças relevantes serão registradas aqui.

O projeto segue versionamento semântico.

## [Unreleased]

### Added

- fundação .NET 10/WPF, SQLite/EF Core, WAL e tooling de migrations;
- smoke WPF automatizado no CI Windows com dados descartáveis, cobrindo bootstrap, autenticação, shell, abas básicas de Compras e logout;
- pacote portátil Windows de QA com publicação `win-x64` self-contained, migration bundle, inicializador seguro de banco novo, metadados de release e hashes SHA-256;
- auditoria automatizada de vulnerabilidades NuGet diretas/transitivas, com parser JSON e teste sintético de rejeição no CI;
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
- entidade `Material` com identidade, nome e status;
- `MaterialService`, `IMaterialRepository` e `EfMaterialRepository`;
- permissões `materials.read` e `materials.manage` no `PermissionCatalog`;
- migration `20261018222000_AddMaterials`;
- testes de domínio, Application, repositório e migration smoke para Materiais;
- catálogo WPF de Materiais com loading/erro/vazio, criação, renomeação e ativação/desativação;
- entidade `Product` separada de `Material`, com identidade, nome e status;
- `ProductService`, `IProductRepository` e `EfProductRepository`;
- permissões `products.read` e `products.manage` no `PermissionCatalog`;
- migration `20261019005000_AddProducts`;
- testes de domínio, Application, repositório e migration smoke para Produtos;
- catálogo WPF de Produtos com loading/erro/vazio, listagem de ativos/inativos, criação, renomeação e ativação/desativação;
- entidade `MeasurementUnit` independente, com identidade, nome e status;
- `MeasurementUnitService`, `IMeasurementUnitRepository` e `EfMeasurementUnitRepository`;
- permissões `units.read` e `units.manage` no `PermissionCatalog`;
- migration `20261019093000_AddMeasurementUnits`;
- testes de domínio, Application, repositório e migration smoke para unidades de medida;
- catálogo WPF de Unidades de Medida com loading/erro/vazio, listagem de ativos/inativos, criação, renomeação e ativação/desativação;
- entidade `Category` global independente, com identidade, nome e status;
- `CategoryService`, `ICategoryRepository` e `EfCategoryRepository`;
- permissões `categories.read` e `categories.manage` no `PermissionCatalog`;
- migration `20261020113000_AddCategories`;
- testes de domínio, Application, repositório e migration smoke para Categorias;
- catálogo WPF de Categorias com loading/erro/vazio, listagem de ativos/inativos, criação, renomeação e ativação/desativação;
- entidade `CostCenter` global independente, com identidade, nome e status;
- `CostCenterService`, `ICostCenterRepository` e `EfCostCenterRepository`;
- permissões `cost-centers.read` e `cost-centers.manage` no `PermissionCatalog`;
- migration `20261021100000_AddCostCenters`;
- testes de domínio, Application, repositório e migration smoke para Centros de Custo;
- catálogo WPF de Centros de Custo com loading/erro/vazio, listagem de ativos/inativos, criação, renomeação e ativação/desativação;
- tratamento global de exceções do shell para `DispatcherUnhandledException`, `AppDomain.UnhandledException` e `TaskScheduler.UnobservedTaskException`;
- primeiro slice vertical de Solicitação de Compra, de Domain a WPF;
- segundo slice de Cotação/RFQ com linhas de origem, fornecedores convidados, respostas, envio, finalização, encerramento e cancelamento;
- permissões `request-for-quotations.read` e `request-for-quotations.manage`;
- migration `20261021132811_AddRequestForQuotations` e persistência de preço unitário SQLite em inteiro escalado de 6 casas;
- workspace WPF de RFQ integrado ao módulo Compras;
- terceiro slice de Comparação de Cotações com projeção Application somente leitura;
- janela WPF `QuotationComparisonWindow` integrada ao RFQ, exibindo fornecedores, estado/moeda e ofertas finalizadas por linha sem ranking ou adjudicação;
- quarto slice de Adjudicação manual com `RequestForQuotationAward`, `RequestForQuotationAwardLine` e estados `Draft/Finalized`;
- permissão `request-for-quotations.award`, separada das permissões de leitura/gestão do RFQ;
- migration `20261022105027_AddRequestForQuotationAwards`, repositório EF e constraints de uma decisão por RFQ/seleções;
- janela WPF `RequestForQuotationAwardWindow` com início explícito, seleção manual por linha e modo somente leitura após finalização, sem criação de Pedido de Compra.

### Changed

- Filiais reutilizam `companies.read` para consulta e `companies.manage` para mutações;
- FK de filial usa `ON DELETE RESTRICT`, evitando exclusão silenciosa de filiais por uma futura exclusão de empresa;
- status de empresa e filial permanecem independentes; não existe cascata implícita;
- Fornecedores usam autorização própria `suppliers.read/manage` na Application;
- Materiais usam autorização própria `materials.read/manage` na Application;
- Produtos usam autorização própria `products.read/manage` na Application;
- Unidades de medida usam autorização própria `units.read/manage` na Application;
- Categorias usam autorização própria `categories.read/manage` na Application;
- Centros de Custo usam autorização própria `cost-centers.read/manage` na Application;
- `Material` e `Product` permanecem conceitos separados e não presumem código/SKU, unidade, categoria, estoque, preço, composição/BOM ou vínculo comercial;
- `MeasurementUnit` permanece independente e não presume sigla/símbolo, precisão, conversões ou vínculo automático com Produto/Material;
- `Category` permanece independente e não presume hierarquia, categoria-pai, código ou vínculo automático com Produto/Material;
- `CostCenter` permanece independente e não presume código, hierarquia, vínculo obrigatório com empresa/filial, regra financeira ou relacionamento automático;
- novas instalações recebem todo o catálogo de permissões no bootstrap; instalações existentes não recebem grants novos automaticamente;
- dados fiscais/endereço de filial e campos fiscais, bancários ou comerciais de fornecedor permanecem fora do modelo até requisito explícito;
- falha não tratada no dispatcher WPF agora é logada como fatal, mostra mensagem genérica e encerra a aplicação para evitar continuidade em estado incerto;
- exceções não observadas de task são registradas e marcadas como observadas; exceções não tratadas do `AppDomain` recebem logging final.

### Validation

- Windows Server 2025;
- .NET SDK 10.0.402 / runtime 10.0.13;
- Release build: 0 warnings / 0 errors;
- benchmark tool: 0 warnings / 0 errors;
- NuGet audit: zero vulnerabilidades conhecidas diretas/transitivas; fixture sintética vulnerável rejeitada;
- suíte automatizada: 271/271;
- Domain 75/75, Application 130/130, Persistence 59/59, Infrastructure 7/7;
- EF Core 10.0.13: sem mudanças pendentes no modelo;
- 17 migrations: apply -> rollback 0 -> reapply passando;
- CI #108 validou o slice completo de Fornecedores;
- CI #109 recompilou o tratamento global de exceções e preservou todo o baseline;
- CI #112 validou o núcleo de Materiais, snapshot EF e a nona migration;
- CI #114 validou o catálogo WPF de Materiais e preservou todo o baseline;
- CI #116 validou o núcleo de Produto, snapshot EF e a décima migration;
- CI #118 validou o catálogo WPF de Produtos e preservou 168/168 testes, snapshot EF e as 10 migrations;
- CI #120 validou o núcleo de unidades de medida com 178/178 testes, snapshot EF e a décima primeira migration;
- CI #122 validou o catálogo WPF de Unidades de Medida e preservou 178/178 testes, snapshot EF e as 11 migrations;
- CI #124 validou o núcleo de Categorias com 188/188 testes, snapshot EF e a décima segunda migration;
- CI #126 validou o catálogo WPF de Categorias e preservou 188/188 testes, snapshot EF e as 12 migrations;
- CI #128 validou o núcleo de Centros de Custo e a décima terceira migration;
- CI #131 validou o catálogo WPF de Centros de Custo e preservou 198/198 testes, snapshot EF e as 13 migrations;
- CI #160 validou o slice vertical de RFQ com 235/235 testes, snapshot EF consistente e 15 migrations em apply -> rollback 0 -> reapply;
- CI #166 validou Comparação de Cotações, compilação WPF, 238/238 testes, snapshot EF limpo e as mesmas 15 migrations;
- CI #188 validou Adjudicação manual de RFQ, compilação WPF, 253/253 testes, snapshot EF limpo e 16 migrations;
- CI técnico #199 validou o smoke WPF automatizado, 271/271 testes, snapshot EF limpo e 17 migrations em apply -> rollback 0 -> reapply;
- CI técnico #204 validou geração do pacote portátil, inicialização de banco novo, recusa de banco existente e smoke WPF sobre o executável/migration bundle publicados;
- CI técnico #207 validou zero vulnerabilidades NuGet conhecidas, rejeição de uma fixture vulnerável, 271/271 testes e todos os gates de WPF, EF/migrations e package;
- smoke WPF automatizado está integrado ao CI; smoke visual/interativo manual em Windows real permanece obrigatório antes de release.

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

## Marco de Materiais — 2026-10-18

- `Material` adicionado como conceito separado de Produto, com identidade, nome e status;
- `materials.read` e `materials.manage` adicionados ao catálogo de permissões;
- `MaterialService`, repositório EF e migration `20261018222000_AddMaterials` integrados;
- baseline subiu para 158 testes e 9 migrations;
- CI #112 validou build, testes, snapshot e ciclo completo de migrations;
- catálogo WPF de Materiais integrado ao módulo `Cadastros`;
- CI #114 recompilou o Desktop e preservou 158/158 testes e 9 migrations.

## Marco de Produto — 2026-10-18

- `Product` adicionado como conceito separado de `Material`, com identidade, nome e status;
- `products.read` e `products.manage` adicionados ao catálogo de permissões;
- `ProductService`, repositório EF e migration `20261019005000_AddProducts` integrados;
- baseline subiu para 168 testes e 10 migrations;
- CI #116 validou build, 168/168 testes, snapshot EF e ciclo completo das 10 migrations;
- catálogo WPF de Produtos integrado ao módulo `Cadastros` com ativos/inativos, criação, renomeação e status;
- CI #118 recompilou o Desktop/XAML e preservou 168/168 testes e 10 migrations.

## Marco de unidades de medida — 2026-10-20

- `MeasurementUnit` adicionado como cadastro global independente, com identidade, nome e status;
- `units.read` e `units.manage` adicionados ao catálogo de permissões;
- `MeasurementUnitService`, repositório EF e migration `20261019093000_AddMeasurementUnits` integrados;
- sem sigla/símbolo, precisão, conversão ou relacionamento automático com Produto/Material;
- baseline subiu para 178 testes e 11 migrations;
- CI #120 validou build, 178/178 testes, snapshot EF e ciclo completo das 11 migrations;
- catálogo WPF de Unidades de Medida integrado ao módulo `Cadastros` com ativos/inativos, criação, renomeação e status;
- CI #122 recompilou o Desktop/XAML e preservou 178/178 testes e 11 migrations.

## Marco de Categorias — 2026-10-20

- `Category` adicionada como cadastro global independente, com identidade, nome e status;
- `categories.read` e `categories.manage` adicionados ao catálogo de permissões;
- `CategoryService`, repositório EF e migration `20261020113000_AddCategories` integrados;
- sem hierarquia, categoria-pai, código ou relacionamento automático com Produto/Material;
- baseline subiu para 188 testes e 12 migrations;
- CI #124 validou build, 188/188 testes, snapshot EF e ciclo completo das 12 migrations;
- catálogo WPF de Categorias integrado ao módulo `Cadastros` com ativos/inativos, criação, renomeação e status;
- CI #126 recompilou o Desktop/XAML e preservou 188/188 testes e 12 migrations.

## Marco de Centros de Custo — 2026-10-21

- `CostCenter` adicionado como cadastro global independente, com identidade, nome e status;
- `cost-centers.read` e `cost-centers.manage` adicionados ao catálogo de permissões;
- `CostCenterService`, repositório EF e migration `20261021100000_AddCostCenters` integrados;
- sem código, hierarquia, vínculo obrigatório com empresa/filial, regra financeira ou relacionamento automático;
- baseline subiu para 198 testes e 13 migrations;
- CI #128 validou o núcleo, snapshot EF e ciclo completo das 13 migrations;
- catálogo WPF de Centros de Custo integrado ao módulo `Cadastros` com ativos/inativos, criação, renomeação e status;
- CI #131 recompilou o Desktop/XAML e preservou 198/198 testes e 13 migrations;
- Fase 6 de cadastros-base concluída; próxima fase exige definição do fluxo mínimo de Compras com regras reais antes de modelar o módulo.
