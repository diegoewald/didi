# Testes

## Estratégia

- Domain: invariantes e value objects;
- Application: casos de uso, autenticação, bootstrap e autorização;
- Persistence: mappings, constraints, migrations, concorrência e queries críticas;
- Infrastructure: filesystem, paths e serviços técnicos;
- Desktop: compilação WPF obrigatória em CI; UI automation somente quando justificar o custo.

## Regra de bugfix

1. reproduzir;
2. criar teste de regressão quando viável;
3. corrigir;
4. executar teste relacionado;
5. executar suíte relevante.

## Validação Windows

O CI permanente usa Windows Server 2025, .NET SDK 10.0.402 e `dotnet-ef` 10.0.13. Executa restore, build Release, build de `LocalErp.PasswordHashBenchmark`, testes, `has-pending-model-changes`, apply de migrations, rollback para zero e reapply.

## Baseline atual — 2026-10-20

- Domain: 42/42;
- Application: 94/94;
- Persistence: 46/46;
- Infrastructure: 6/6;
- Total automatizado: 188/188;
- Build Release: 0 warnings / 0 errors;
- Benchmark tool: 0 warnings / 0 errors;
- EF Core: sem mudanças pendentes no modelo;
- Migrations: 12; apply -> rollback 0 -> reapply passando.

### Cobertura de empresa/filial

- domínio de `CompanyBranch`: normalização do nome, `CompanyId` obrigatório, rename e status;
- Application: criação exige empresa existente; listagem exige `companies.read`; criação/rename/status exigem `companies.manage`;
- listagem de filiais fica restrita à empresa solicitada;
- Persistence: insert/list/filter/update por empresa e status;
- FK SQLite rejeita filial apontando para empresa inexistente;
- migration `20261018105000_AddCompanyBranches` entra no ciclo completo de migration smoke.

### Cobertura de fornecedores

- domínio de `Supplier`: nome obrigatório/normalizado, limite de 200 caracteres, rename e status;
- Application: listagem exige `suppliers.read`; criação, rename e status exigem `suppliers.manage`;
- Persistence: add/get/list/update e filtro de inativos sobre SQLite;
- `PermissionCatalog` inclui `suppliers.read` e `suppliers.manage`;
- migration `20261018142500_AddSuppliers` integra o snapshot e o ciclo completo de migration smoke;
- CI #108 validou backend + catálogo WPF de Fornecedores com 148/148 testes.

### Cobertura de Materiais

- domínio de `Material`: nome obrigatório/normalizado, limite de 200 caracteres, rename e status;
- Application: listagem exige `materials.read`; criação, rename e status exigem `materials.manage`;
- Application rejeita identificador inexistente sem persistir mutação;
- Persistence: add/get/list/update, ordenação e filtro de inativos sobre SQLite;
- `PermissionCatalog` inclui `materials.read` e `materials.manage`, que entram no bootstrap de instalações novas;
- migration `20261018222000_AddMaterials` integra o snapshot e o ciclo completo de migration smoke;
- CI #112 validou o núcleo de Materiais com 158/158 testes e 9 migrations;
- CI #114 recompilou `MaterialsCatalogView`, integração no `MainWindow` e DI, mantendo 158/158 testes e as 9 migrations verdes.

### Cobertura de Produtos

- domínio de `Product`: nome obrigatório/normalizado, limite de 200 caracteres, rename e status;
- Application: listagem exige `products.read`; criação, rename e status exigem `products.manage`;
- Application rejeita identificador inexistente sem persistir mutação;
- Persistence: add/get/list/update, ordenação e filtro de inativos sobre SQLite;
- `PermissionCatalog` inclui `products.read` e `products.manage`, incorporados ao bootstrap de instalações novas;
- migration `20261019005000_AddProducts` integra o snapshot e o migration smoke;
- CI #116 validou o núcleo de Produto com 168/168 testes e 10 migrations;
- CI #118 recompilou `ProductsCatalogView`, integração no `MainWindow`, DI e ViewModel, preservando 168/168 testes e as 10 migrations verdes.

### Cobertura de unidades de medida

- domínio de `MeasurementUnit`: nome obrigatório/normalizado, limite de 200 caracteres, rename e status;
- Application: listagem exige `units.read`; criação, rename e status exigem `units.manage`;
- Application rejeita identificador inexistente sem persistir mutação;
- Persistence: add/get/list/update, ordenação e filtro de inativos sobre SQLite;
- `PermissionCatalog` inclui `units.read` e `units.manage`, incorporados ao bootstrap de instalações novas;
- migration `20261019093000_AddMeasurementUnits` integra o snapshot e o migration smoke;
- CI #120 validou o núcleo de unidade de medida com 178/178 testes e 11 migrations;
- CI #122 recompilou `MeasurementUnitsCatalogView`, integração no `MainWindow`, DI e ViewModel, preservando 178/178 testes e as 11 migrations verdes.

### Cobertura de Categorias

- domínio de `Category`: nome obrigatório/normalizado, limite de 200 caracteres, rename e status;
- Application: listagem exige `categories.read`; criação, rename e status exigem `categories.manage`;
- Application rejeita identificador inexistente sem persistir mutação;
- Persistence: add/get/list/update, ordenação e filtro de inativos sobre SQLite;
- `PermissionCatalog` inclui `categories.read` e `categories.manage`, incorporados ao bootstrap de instalações novas;
- migration `20261020113000_AddCategories` integra o snapshot e o migration smoke;
- CI #124 validou o núcleo de Categorias com 188/188 testes e 12 migrations;
- CI #126 recompilou `CategoriesCatalogView`, integração no `MainWindow`, DI e ViewModel, preservando 188/188 testes e as 12 migrations verdes.

A compilação WPF valida XAML, handlers, composição de tipos e DI compilável, mas não substitui smoke visual/interativo em Windows real.

### Cobertura de robustez do shell

O tratamento global de exceções é validado pelo build WPF do CI. O CI #109 recompilou `DispatcherUnhandledException`, `AppDomain.UnhandledException` e `TaskScheduler.UnobservedTaskException`. O comportamento visual/fatal ainda exige smoke manual controlado em Windows real.

### Cobertura de segurança mantida

O baseline continua cobrindo bootstrap concorrente, throttling/lockout, dummy verify, auditoria de autenticação, administração de usuários, salvaguardas de `roles.manage`, auditoria administrativa, troca própria de senha e compare-and-set concorrente de credenciais.

## Benchmark Windows CI

Referência com `MicrosoftPasswordHashService`, `IterationCount = 220000`, 2 warmups e 20 amostras:

- Hash: mediana 145.155 ms;
- Verify Success: mediana 138.080 ms;
- Verify Failure: mediana 141.959 ms.

Esses números validam o tooling, não representam automaticamente o hardware final.

## Pendente antes de release

Executar smoke manual em Windows incluindo:

- bootstrap/login/logout e throttling;
- Administração de usuários/roles/permissões;
- troca própria de senha e limpeza dos `PasswordBox`;
- catálogo de Empresas e janela de Filiais;
- catálogo de Fornecedores: loading/erro/vazio, criação, rename, ativação/desativação e permissões insuficientes;
- catálogo de Materiais: loading/erro/vazio, criação, rename com Enter/Escape, ativação/desativação, confirmação e `materials.read/manage` insuficientes;
- catálogo de Produtos: loading/erro/vazio, criação, rename com Enter/Escape, ativação/desativação, confirmação e `products.read/manage` insuficientes;
- catálogo de Unidades de Medida: loading/erro/vazio, criação, rename com Enter/Escape, ativação/desativação, confirmação e `units.read/manage` insuficientes;
- catálogo de Categorias: loading/erro/vazio, criação, rename com Enter/Escape, ativação/desativação, confirmação e `categories.read/manage` insuficientes;
- verificar que desativar empresa não modifica automaticamente filiais;
- foco, Enter, fechamento durante mutação, DPI e aparência em Windows real;
- em build de diagnóstico, validar que uma exceção não tratada do dispatcher gera log, mensagem genérica e encerramento controlado;
- benchmark de senha em hardware representativo.
