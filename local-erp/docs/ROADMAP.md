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
- [-] estados loading/empty/error já existem em Administração, Cadastros, Solicitações de Compra e RFQ, ainda sem componente comum;
- [ ] componentes reutilizáveis de tabela/filtro quando repetição justificar;
- [ ] smoke visual/interativo em Windows real.

## Fase 5 — Administração de usuários e roles

- [x] administração de usuários Partes A–F;
- [x] roles/permissões completas e salvaguarda da última capacidade efetiva;
- [x] auditoria atômica das mutações administrativas;
- [x] troca própria de senha com senha atual, compare-and-set transacional e auditoria.

## Fase 6 — Cadastros-base

- [x] empresa: listagem, criação, renomeação e status;
- [x] filial: `CompanyBranch` vinculada obrigatoriamente à empresa;
- [x] filial: persistência, migration, autorização e UI operacional;
- [x] fornecedor: domínio, persistência, migration, permissões e UI operacional;
- [x] material: domínio, Application, persistência, migration, permissões e UI operacional;
- [x] produto: domínio, Application, persistência, migration, permissões e UI operacional;
- [x] unidade de medida: domínio, Application, persistência, migration, permissões e UI operacional;
- [x] categoria: domínio, Application, persistência, migration, permissões e UI operacional;
- [x] centro de custo: domínio, Application, persistência, migration, permissões e UI operacional.

## Fase 7 — Primeiro módulo operacional

- [x] compras: descoberta inicial documentada em `docs/PURCHASING.md`, separando padrões de mercado de regras não confirmadas;
- [x] compras: primeiro slice de Solicitação de Compra especificado em `docs/PURCHASE-REQUEST-SPEC.md`;
- [x] compras: domínio e Application da Solicitação de Compra implementados com estados `Draft/Submitted/Cancelled`, permissões e invariantes;
- [x] compras: persistência EF, FKs, índices, repositório de agregado e migration `20261021123000_AddPurchaseRequests` validados;
- [x] compras: primeira UI WPF operacional da Solicitação de Compra integrada ao shell;
- [x] compras: segundo slice de Cotação/RFQ especificado em `docs/REQUEST-FOR-QUOTATION-SPEC.md`;
- [x] compras: domínio e Application de RFQ/respostas implementados com permissões `request-for-quotations.read/manage`;
- [x] compras: persistência EF e migration `20261021132811_AddRequestForQuotations` validadas, incluindo preço unitário como inteiro escalado de 6 casas no SQLite;
- [x] compras: workspace WPF de RFQ integrado ao shell, com origem em Solicitação `Submitted`, linhas, fornecedores, respostas, envio, finalização, encerramento e cancelamento;
- [x] compras: terceiro slice de Comparação de Cotações especificado em `docs/QUOTATION-COMPARISON-SPEC.md`;
- [x] compras: projeção Application de comparação somente leitura implementada com `request-for-quotations.read`, respostas `Draft` excluídas das ofertas e ordenação determinística;
- [x] compras: janela WPF de comparação integrada ao RFQ, com fornecedores, estado/moeda e matriz por linha sem ranking ou seleção automática;
- [x] compras: quarto slice de Adjudicação manual especificado em `docs/RFQ-AWARD-SPEC.md`;
- [x] compras: Domain/Application de decisão `Draft/Finalized` implementados com permissão específica `request-for-quotations.award`;
- [x] compras: persistência EF e migration `20261022105027_AddRequestForQuotationAwards` validadas com unicidade/FKs e migration smoke;
- [x] compras: janela WPF de adjudicação integrada ao RFQ `Closed`, com seleção manual por linha, linhas opcionais sem adjudicação e modo somente leitura após finalização;
- [x] compras: quinto slice de Pedido de Compra especificado em `docs/PURCHASE-ORDER-SPEC.md`;
- [x] compras: Domain/Application de Pedido de Compra implementados com snapshots, estados `Draft/Issued/Cancelled` e permissões `purchase-orders.read/manage`;
- [x] compras: persistência EF e migration `20261022154117_AddPurchaseOrders` validadas, com FKs/unicidades, preço escalado e sequência protegida por transação imediata SQLite;
- [ ] compras: integrar workspace WPF de Pedido de Compra ao módulo Compras.

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

## Marco de unidades de medida — 2026-10-20

`MeasurementUnit` é um cadastro global independente, contendo somente identidade, nome normalizado de até 200 caracteres e status ativo/inativo. Não existe, nesta etapa, sigla/símbolo, precisão decimal, fator de conversão ou relação automática com `Product` ou `Material`.

`MeasurementUnitService` exige `units.read` para consulta e `units.manage` para criação, renomeação e status. As permissões entram no `PermissionCatalog` e no bootstrap de instalações novas; roles existentes não recebem grants adicionais automaticamente.

`EfMeasurementUnitRepository` persiste `MeasurementUnits`. A migration `20261019093000_AddMeasurementUnits` é a décima primeira migration. O CI #120 validou o núcleo com 178/178 testes (Domain 38, Application 89, Persistence 45, Infrastructure 6), snapshot EF sem mudanças pendentes e 11 migrations em apply -> rollback 0 -> reapply.

O catálogo WPF de Unidades de Medida foi integrado ao módulo `Cadastros` com loading/erro/vazio, listagem de ativos/inativos, criação, renomeação via Enter/Escape e ativação/desativação com confirmação. O CI #122 recompilou o Desktop/XAML e preservou 178/178 testes, snapshot EF e o ciclo completo das 11 migrations.

## Marco de Categorias — 2026-10-20

`Category` foi introduzida como cadastro global independente, contendo somente identidade, nome normalizado de até 200 caracteres e status ativo/inativo. Não existe hierarquia, categoria-pai, código ou relacionamento automático com `Product` ou `Material` nesta etapa.

`CategoryService` exige `categories.read` para consulta e `categories.manage` para criação, renomeação e status. As permissões entram no `PermissionCatalog` e no bootstrap de instalações novas; roles existentes não recebem grants adicionais automaticamente.

`EfCategoryRepository` persiste `Categories`. A migration `20261020113000_AddCategories` é a décima segunda migration. O CI #124 validou o núcleo com 188/188 testes (Domain 42, Application 94, Persistence 46, Infrastructure 6), snapshot EF sem mudanças pendentes e 12 migrations em apply -> rollback 0 -> reapply.

O catálogo WPF de Categorias foi integrado ao módulo `Cadastros` com loading/erro/vazio, listagem de ativos/inativos, criação, renomeação via Enter/Escape e ativação/desativação com confirmação. O CI #126 recompilou o Desktop/XAML e preservou 188/188 testes, snapshot EF e o ciclo completo das 12 migrations.

## Marco de Centros de Custo — 2026-10-21

`CostCenter` é um cadastro global independente com identidade, nome normalizado de até 200 caracteres e status ativo/inativo. Código, hierarquia, vínculo obrigatório com empresa/filial, regra financeira e relacionamentos automáticos permanecem fora do modelo até existir requisito explícito.

`CostCenterService` exige `cost-centers.read` para consulta e `cost-centers.manage` para criação, renomeação e status. As permissões entram no `PermissionCatalog` e no bootstrap de instalações novas; roles existentes continuam sem receber grants novos automaticamente.

`EfCostCenterRepository` persiste `CostCenters`. A migration `20261021100000_AddCostCenters` é a décima terceira migration.

O catálogo WPF de Centros de Custo foi integrado ao módulo `Cadastros` com loading/erro/vazio, listagem de ativos/inativos, criação, renomeação via Enter/Escape e ativação/desativação com confirmação. O CI #131 recompilou o Desktop/XAML e validou 198/198 testes (Domain 46, Application 99, Persistence 47, Infrastructure 6), snapshot EF sem mudanças pendentes e 13 migrations em apply -> rollback 0 -> reapply.

Com isso, a Fase 6 de cadastros-base está concluída.

## Marco de descoberta de Compras — 2026-10-21

A Fase 7 foi iniciada pela documentação `docs/PURCHASING.md`. Foram comparados padrões de referência de Microsoft Dynamics 365, Oracle Procurement e SAP para separar conceitos recorrentes de regras que variam por organização.

O projeto reconhece como cadeia conceitual de longo prazo `necessidade interna -> requisição -> processo de compra -> pedido -> acompanhamento -> recebimento`, mas não tornou obrigatórios cotação, aprovação, quantidade mínima de fornecedores, alçadas, vínculos organizacionais ou recebimento parcial.

O primeiro slice é **Solicitação de Compra**. A especificação executável está em `docs/PURCHASE-REQUEST-SPEC.md`: estados `Draft`, `Submitted` e `Cancelled`; permissões `purchase-requests.read/manage`; linha por descrição, quantidade e Unidade de Medida; nenhuma aprovação, cotação, fornecedor, preço, pedido ou recebimento neste primeiro corte; edição somente em `Draft`; cancelamento rastreável e ausência de hard delete público.

## Marco do núcleo e persistência de Solicitação de Compra — 2026-10-21

`PurchaseRequest` e `PurchaseRequestLine` foram implementados como agregado de domínio. A Application oferece criação/listagem/consulta, edição de rascunho e linhas, envio e cancelamento, mantendo autorização e validação de Unidade de Medida fora da UI. As mutações do domínio validam antes de alterar estado, evitando alterações parciais em memória em caso de falha.

`EfPurchaseRequestRepository` persiste o agregado em `PurchaseRequests` e `PurchaseRequestLines`, sincronizando filhos em um único `SaveChangesAsync`. Usuários e Unidade de Medida usam FKs `RESTRICT`; as linhas usam `CASCADE` somente em relação ao pai interno do agregado. A migration `20261021123000_AddPurchaseRequests` é a 14ª migration.

O CI #143 validou 216/216 testes (Domain 56, Application 105, Persistence 49, Infrastructure 6), build Release sem warnings/erros, snapshot EF sem mudanças pendentes e as 14 migrations em apply -> rollback 0 -> reapply.

## Marco da UI WPF de Solicitação de Compra — 2026-10-21

O módulo `Compras` passou a abrir um workspace operacional de Solicitações de Compra. A tela oferece filtro por `Draft`/`Submitted`/`Cancelled`, criação e abertura de rascunhos, edição de observação, inclusão/edição/remoção de linhas, seleção de Unidade de Medida ativa, envio e cancelamento com justificativa. Solicitações `Submitted` e `Cancelled` exibem conteúdo em modo somente leitura, respeitando as transições definidas no domínio.

A UI não recebeu regras de autorização próprias: consultas e mutações continuam passando por `PurchaseRequestService`. O contexto de Compras carrega o nome de Unidades de Medida ativas e inativas para preservar leitura histórica, mas somente unidades ativas são oferecidas no editor de linha. Isso evita que uma unidade desativada posteriormente torne uma solicitação antiga ilegível e não exige conceder `units.read` ao operador de Compras.

O CI #148 validou o workspace WPF com **217/217 testes** (Domain 56, Application 106, Persistence 49, Infrastructure 6), build Release e benchmark tool com 0 warnings/erros, snapshot EF sem mudanças pendentes e as 14 migrations em apply -> rollback 0 -> reapply.

Com isso, o primeiro slice vertical de **Solicitação de Compra** está implementado de Domain a WPF. A próxima prioridade funcional é especificar o próximo slice de Compras — aprovação, cotação e/ou Pedido de Compra — antes de introduzir entidades, estados ou vínculos adicionais. O smoke visual/interativo em Windows real permanece uma pendência de pré-release separada.

## Marco de Cotação / RFQ — 2026-10-21

O segundo slice vertical de Compras foi definido em `docs/REQUEST-FOR-QUOTATION-SPEC.md` e implementado de Domain a WPF. `RequestForQuotation` parte obrigatoriamente de uma Solicitação de Compra `Submitted`, seleciona linhas da origem, mantém fornecedores convidados e controla estados `Draft`, `Sent`, `Closed` e `Cancelled`. Respostas de fornecedor usam `Draft`/`Finalized`, moeda explícita e preço unitário com até seis casas decimais.

A persistência usa a migration `20261021132811_AddRequestForQuotations`. O preço unitário segue o ADR-0005 e é armazenado no SQLite como inteiro escalado de 6 casas, sem `double`. O workspace WPF permite listar/filtrar RFQs, criar a partir de Solicitação enviada, administrar linhas e fornecedores em rascunho, enviar, registrar/finalizar respostas, encerrar e cancelar. Autorização e invariantes permanecem na Application/Domain.

O CI #160 validou **235/235 testes** (Domain 63, Application 115, Persistence 51, Infrastructure 6), build Release e benchmark tool com 0 warnings/erros, snapshot EF sem mudanças pendentes e **15 migrations** em apply -> rollback 0 -> reapply.

O slice não define vencedor, menor preço obrigatório, quantidade mínima de fornecedores, aprovação, Pedido de Compra, impostos, frete, câmbio, estoque, financeiro ou fiscal. A próxima prioridade funcional é especificar o **terceiro slice de Compras** antes de introduzir qualquer uma dessas regras.

## Marco de Comparação de Cotações — 2026-10-21

O terceiro slice vertical de Compras foi implementado como projeção somente leitura sobre RFQ, Solicitação de origem, fornecedores e respostas existentes. `RequestForQuotationComparisonService` exige `request-for-quotations.read`, preserva fornecedores sem resposta, usa somente respostas `Finalized` como ofertas comparáveis e mantém moedas distintas sem conversão ou ranking.

A UI WPF abre `QuotationComparisonWindow` a partir do RFQ selecionado e apresenta cabeçalho, fornecedores convidados e matriz por linha com preço/data prometida, sem qualquer comando de adjudicação, aceite ou geração de Pedido de Compra. Nenhuma entidade, tabela, coluna ou migration foi adicionada por este slice.

O CI #166 validou **238/238 testes** (Domain 63, Application 118, Persistence 51, Infrastructure 6), build Release e benchmark tool com 0 warnings/erros, snapshot EF limpo e 15 migrations em apply -> rollback 0 -> reapply.

A próxima prioridade funcional é especificar o quarto slice de **decisão/adjudicação** e sua relação com **Pedido de Compra**, incluindo granularidade da escolha, possibilidade de múltiplos fornecedores, justificativa/histórico e transição para pedido, antes de persistir qualquer decisão.


## Marco de Adjudicação manual de RFQ — 2026-10-22

O quarto slice vertical de Compras registra uma decisão manual após o RFQ `Closed`. `RequestForQuotationAward` usa estados `Draft/Finalized`; cada linha do RFQ pode permanecer sem seleção ou apontar para exatamente uma `SupplierQuotationLine` pertencente a uma resposta `Finalized` da mesma linha e do mesmo RFQ. Não existe split de quantidade, ranking, menor preço automático, conversão cambial ou aprovação.

A permissão de mutação é `request-for-quotations.award`, separada de `request-for-quotations.manage`; leitura continua usando `request-for-quotations.read`. A migration `20261022105027_AddRequestForQuotationAwards` é a 16ª migration e garante uma decisão por RFQ, referências externas `RESTRICT` e unicidade das seleções.

`RequestForQuotationAwardWindow` oferece início explícito da decisão, observação, seleção por linha e finalização. Nenhuma opção é escolhida automaticamente e a decisão finalizada fica somente leitura. O CI #188 validou **253/253 testes** (Domain 69, Application 124, Persistence 54, Infrastructure 6), build WPF sem warnings/erros, snapshot EF limpo e 16 migrations em apply -> rollback 0 -> reapply.

## Marco do núcleo e persistência de Pedido de Compra — 2026-10-22

O quinto slice vertical de Compras transforma adjudicação `Finalized` em `PurchaseOrder` de um único fornecedor. O agregado preserva snapshots de fornecedor, descrição, quantidade, unidade, preço e data prometida; controla `Draft/Issued/Cancelled`; permite somente observação em rascunho; e mantém emissão/cancelamento rastreáveis sem introduzir aprovação, recebimento, estoque, financeiro ou fiscal.

`SqlitePurchaseOrderRepository` reserva `SequenceNumber` crescente dentro de transação SQLite imediata, evitando `MAX + 1` desprotegido. A persistência garante unicidade de sequência, de `(RequestForQuotationAwardId, SupplierId)` e de `RequestForQuotationAwardLineId`; `UnitPrice` segue o ADR-0005 como inteiro escalado de seis casas. A migration `20261022154117_AddPurchaseOrders` é a 17ª migration.

A validação Windows passou com **269/269 testes** (Domain 75, Application 129, Persistence 59, Infrastructure 6), build Release/benchmark sem warnings ou erros, snapshot EF limpo e 17 migrations em apply -> rollback 0 -> reapply. A próxima prioridade é a **UI WPF de Pedido de Compra**; recebimento continua fora do escopo até especificação própria.
