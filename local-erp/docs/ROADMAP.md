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
- [x] tratamento global de erro além do startup;
- [ ] atalhos e foco/teclado;
- [-] estados loading/empty/error já existem em Administração, Empresas, Filiais, Fornecedores, Materiais e Produtos, ainda sem componente comum;
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
- [x] fornecedor: domínio, persistência, migration, permissões e UI operacional;
- [x] material: domínio, Application, persistência, migration, permissões e UI operacional;
- [x] produto: domínio, Application, persistência, migration, permissões e UI operacional;
- [-] unidade de medida: domínio, Application, persistência, migration e permissões concluídos; UI WPF pendente;
- [ ] categorias;
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

`CompanyBranchService` exige `companies.read` para listagem e `companies.manage` para criação, renomeação e status. A tela `Empresas` abre `CompanyBranchesWindow` por empresa. Dados fiscais, endereço, códigos internos e conceito de matriz não foram presumidos.

A migration `20261018105000_AddCompanyBranches` adiciona tabela, FK e índice. A suíte chegou a 138/138 testes e 7 migrations.

## Marco de fornecedores — 2026-10-18

`Supplier` foi introduzido com identidade, nome normalizado de até 200 caracteres e status ativo/inativo. `SupplierService` usa `suppliers.read` para consulta e `suppliers.manage` para criação, renomeação e status; as novas permissões entram no `PermissionCatalog` e, portanto, também no bootstrap do primeiro administrador.

`EfSupplierRepository` persiste o cadastro em `Suppliers`. A migration `20261018142500_AddSuppliers` integra a oitava migration do projeto. O catálogo WPF lista ativos/inativos e permite criar, renomear, ativar e desativar sem mover autorização para a UI.

O CI #108 validou o slice completo com 148/148 testes (Domain 26, Application 74, Persistence 42, Infrastructure 6), snapshot EF consistente e 8 migrations em apply -> rollback 0 -> reapply.

## Marco de robustez do shell — 2026-10-18

O `App` passou a registrar tratamento central para `DispatcherUnhandledException`, `AppDomain.UnhandledException` e `TaskScheduler.UnobservedTaskException`. Falha não tratada no dispatcher é registrada como fatal, apresenta mensagem genérica e encerra a aplicação; tasks não observadas são registradas sem expor detalhes ao usuário.

O CI #109 recompilou o WPF e manteve 148/148 testes, snapshot EF e as 8 migrations verdes. O tratamento global é uma última barreira de observabilidade/encerramento seguro e não substitui tratamento local de erros recuperáveis.

## Marco de Materiais — 2026-10-18

`Material` foi modelado como conceito independente de Produto, conforme a especificação mestre. O modelo atual contém somente identidade, nome normalizado de até 200 caracteres e status ativo/inativo; código/SKU, unidade, categoria, estoque, preço e vínculos comerciais permanecem fora do escopo até existir requisito explícito.

`MaterialService` exige `materials.read` para listagem e `materials.manage` para criação, renomeação e status. Novas instalações recebem essas permissões pelo bootstrap; instalações existentes mantêm grants explícitos e não recebem elevação automática de acesso.

`EfMaterialRepository` persiste `Materials`. A migration `20261018222000_AddMaterials` é a nona migration. O CI #112 validou 158/158 testes (Domain 30, Application 79, Persistence 43, Infrastructure 6), snapshot EF consistente e 9 migrations em apply -> rollback 0 -> reapply.

O catálogo WPF de Materiais foi integrado ao módulo `Cadastros` com estados loading/erro/vazio, criação, renomeação e ativação/desativação. O CI #114 recompilou o Desktop no Windows e preservou 158/158 testes, snapshot EF e o ciclo das 9 migrations.

## Marco de Produto — 2026-10-18

`Product` foi introduzido como conceito independente de `Material`, contendo somente identidade, nome normalizado de até 200 caracteres e status ativo/inativo. Não existe relação Produto-Material, composição/BOM, código/SKU, unidade, categoria, estoque, preço ou regra fiscal/comercial nesta etapa.

`ProductService` exige `products.read` para consulta e `products.manage` para criação, renomeação e status. As permissões entram no `PermissionCatalog` e no bootstrap de novas instalações; roles já existentes não recebem grants adicionais automaticamente.

`EfProductRepository` persiste `Products`. A migration `20261019005000_AddProducts` é a décima migration. O CI #116 validou 168/168 testes (Domain 34, Application 84, Persistence 44, Infrastructure 6), snapshot EF sem mudanças pendentes e 10 migrations em apply -> rollback 0 -> reapply.

O catálogo WPF de Produtos foi integrado ao módulo `Cadastros` com loading/erro/vazio, listagem de ativos/inativos, criação, renomeação via Enter/Escape e ativação/desativação com confirmação. O CI #118 recompilou o Desktop/XAML e preservou 168/168 testes, snapshot EF e o ciclo completo das 10 migrations.

## Marco do núcleo de unidades de medida — 2026-10-19

`MeasurementUnit` foi introduzido como cadastro global independente, contendo somente identidade, nome normalizado de até 200 caracteres e status ativo/inativo. Não existe, nesta etapa, sigla/símbolo, precisão decimal, fator de conversão ou relação automática com `Product` ou `Material`.

`MeasurementUnitService` exige `units.read` para consulta e `units.manage` para criação, renomeação e status. As permissões entram no `PermissionCatalog` e no bootstrap de instalações novas; roles existentes não recebem grants adicionais automaticamente.

`EfMeasurementUnitRepository` persiste `MeasurementUnits`. A migration `20261019093000_AddMeasurementUnits` é a décima primeira migration. O CI #120 validou 178/178 testes (Domain 38, Application 89, Persistence 45, Infrastructure 6), snapshot EF sem mudanças pendentes e 11 migrations em apply -> rollback 0 -> reapply.

Próxima prioridade funcional: **catálogo WPF de unidades de medida**. Categorias permanece uma frente separada e centros de custo continuam posteriores.
