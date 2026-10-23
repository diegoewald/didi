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

O CI permanente usa Windows Server 2025, .NET SDK 10.0.402 e `dotnet-ef` 10.0.13. Executa restore, auditoria NuGet direta/transitiva com gate de rejeição sintética, build Release, build de `LocalErp.PasswordHashBenchmark`, testes, smoke WPF, `has-pending-model-changes`, apply de migrations, rollback para zero e reapply. Depois gera o pacote portátil Windows self-contained, valida a inicialização de banco novo e executa novamente o smoke usando o executável e o migration bundle publicados.

## Baseline atual — 2026-10-23

- Domain: 75/75;
- Application: 130/130;
- Persistence: 59/59;
- Infrastructure: 7/7;
- Total automatizado: 271/271;
- Build Release: 0 warnings / 0 errors;
- Benchmark tool: 0 warnings / 0 errors;
- NuGet audit: nenhuma vulnerabilidade conhecida em dependências diretas ou transitivas; fixture sintética vulnerável rejeitada pelo gate;
- EF Core: sem mudanças pendentes no modelo;
- Migrations: 17; apply -> rollback 0 -> reapply passando.
- Package QA: `win-x64` self-contained + migration bundle + hashes; inicialização de banco novo e smoke do binário publicado passando.

### Auditoria NuGet automatizada

Após o restore, `scripts/check-vulnerable-packages.ps1 -NoRestore` executa `dotnet package list --vulnerable --include-transitive --format json`, interpreta o relatório e falha se qualquer pacote direto ou transitivo possuir advisory. Projetos limpos podem aparecer no JSON sem coleção `frameworks`; o parser trata essa forma como zero achados sem relaxar validação de relatório ausente ou inválido.

O CI executa ainda uma fixture JSON com uma dependência sintética `high` e considera sucesso somente quando o script a rejeita. O CI técnico #207 validou simultaneamente o caso limpo real e o caminho de falha sintético.

### Smoke WPF automatizado

Depois da suíte, o CI Windows executa `scripts/smoke-wpf.ps1`. O script usa `LOCALERP_DATA_ROOT` para isolar todos os dados em um diretório temporário, aplica as migrations reais em um banco descartável e usa Windows UI Automation para percorrer bootstrap -> Main -> Compras -> Solicitações/Cotações/Pedidos -> logout -> login explícito -> Main -> logout. O log precisa registrar as rotas de startup esperadas e não pode conter entrada fatal.

Esse gate valida inicialização, DI, autenticação e navegação básica, mas não substitui o smoke manual de ergonomia, foco, DPI ou o fluxo completo de Compras com dados. Detalhes: `docs/WPF-SMOKE.md`.

### Pacote Windows de QA automatizado

`scripts/package-release.ps1` publica o WPF como `win-x64` self-contained, gera `LocalErp.DatabaseMigrator.exe` com `dotnet ef migrations bundle`, cria `release-info.json`, `SHA256SUMS.txt`, o ZIP e o SHA-256 externo. O CI valida `Initialize-FreshDatabase.ps1` em base descartável e exige que uma segunda execução recuse modificar banco já existente.

Em seguida, o mesmo `scripts/smoke-wpf.ps1` é executado contra `app/LocalErp.Desktop.exe` e contra o migration bundle que estão dentro do pacote. Assim, o gate cobre a distribuição real de QA em vez de validar somente `bin/Release`. Esse artefato continua sem assinatura e não substitui o futuro instalador Velopack nem o fluxo seguro de upgrade de banco existente. Detalhes: `docs/PACKAGING.md`.

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

### Cobertura de Centros de Custo

- domínio de `CostCenter`: nome obrigatório/normalizado, limite de 200 caracteres, rename e status;
- Application: listagem exige `cost-centers.read`; criação, rename e status exigem `cost-centers.manage`;
- Application rejeita identificador inexistente sem persistir mutação;
- Persistence: add/get/list/update, ordenação e filtro de inativos sobre SQLite;
- `PermissionCatalog` inclui `cost-centers.read` e `cost-centers.manage`, incorporados ao bootstrap de instalações novas;
- migration `20261021100000_AddCostCenters` integra o snapshot e o migration smoke;
- CI #128 validou o núcleo de Centros de Custo e a décima terceira migration;
- CI #131 recompilou `CostCentersCatalogView`, integração no `MainWindow`, DI e ViewModel, preservando 198/198 testes e as 13 migrations verdes.

### Cobertura de Solicitação de Compra e RFQ

- Solicitação de Compra: domínio, autorização, unidades ativas, persistência do agregado, FKs, migration e workspace WPF;
- RFQ: estados `Draft/Sent/Closed/Cancelled`, linhas de origem, fornecedores convidados e invariantes de envio/encerramento/cancelamento;
- respostas de fornecedor: `Draft/Finalized`, moeda explícita, preço com até seis casas, linhas cotadas e imutabilidade após finalização;
- Application: permissões `request-for-quotations.read/manage`, validação de Solicitação `Submitted`, fornecedor ativo e referências pertencentes ao agregado;
- Persistence: round trip do agregado, FKs/unicidade e armazenamento exato de `UnitPrice` em inteiro escalado;
- migration `20261021132811_AddRequestForQuotations` integrada ao snapshot e ao migration smoke;
- CI #160 validou o slice vertical de RFQ com 235/235 testes e 15 migrations;
- comparação de cotações: projeção autorizada, fornecedores sem resposta preservados, preços de `Draft` ignorados, moedas não convertidas e matriz por linha;
- CI #166 validou a janela WPF `QuotationComparisonWindow`, **238/238 testes** e as mesmas 15 migrations sem mudança de modelo;
- adjudicação: domínio `Draft/Finalized`, autorização específica, validação de oferta finalizada da mesma linha/RFQ, persistência com uma decisão por RFQ e WPF manual sem pré-seleção;
- CI #188 validou `RequestForQuotationAwardWindow`, **253/253 testes** e 16 migrations com snapshot EF limpo.
A compilação WPF valida XAML, handlers, composição de tipos e DI compilável; o smoke WPF automatizado valida bootstrap, autenticação, shell e navegação básica de Compras. Ambos não substituem o smoke visual/interativo manual em Windows real.

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
- catálogo de Centros de Custo: loading/erro/vazio, criação, rename com Enter/Escape, ativação/desativação, confirmação e `cost-centers.read/manage` insuficientes;
- Solicitação de Compra: lista/filtro, rascunho, linhas, envio, cancelamento, unidade inativa histórica e permissões insuficientes;
- Cotação/RFQ: criação a partir de Solicitação `Submitted`, linhas, fornecedores, envio, respostas, preços/datas, finalização, encerramento/cancelamento e permissões insuficientes;
- Comparação de Cotações: abertura a partir do RFQ, fornecedores sem resposta, resposta `Draft` sem preço comparável, moedas distintas, linhas sem oferta e rolagem horizontal;
- Adjudicação: RFQ `Closed`, início explícito, “Sem adjudicação”, seleção/substituição/remoção por linha, permissão insuficiente, finalização e modo somente leitura;
- Pedido de Compra: criação a partir de adjudicação finalizada, fornecedor elegível, linhas/totais imutáveis, observação em Draft, emissão interna, cancelamento com justificativa e modo somente leitura;
- verificar que desativar empresa não modifica automaticamente filiais;
- foco, Enter, fechamento durante mutação, DPI e aparência em Windows real;
- em build de diagnóstico, validar que uma exceção não tratada do dispatcher gera log, mensagem genérica e encerramento controlado;
- benchmark de senha em hardware representativo.
