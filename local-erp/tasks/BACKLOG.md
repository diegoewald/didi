# Backlog

## P0 — Pré-release / validação de ambiente

- [x] executar smoke automatizado do WPF no CI Windows cobrindo bootstrap, autenticação, shell, abas de Compras e logout com banco descartável;
- [ ] executar smoke visual/interativo manual do WPF em Windows real, incluindo Solicitação de Compra, RFQ, Comparação de Cotações, Adjudicação e Pedido de Compra;
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
- [x] unidade de medida: catálogo WPF operacional em `Cadastros`, com ativos/inativos, criação, renomeação e status;
- [ ] unidade de medida: não adicionar sigla/símbolo, precisão, fatores de conversão ou vínculo com Produto/Material sem requisito explícito;
- [x] categoria: entidade `Category` global independente, com identidade, nome e status;
- [x] categoria: `categories.read` e `categories.manage` registrados no `PermissionCatalog`;
- [x] categoria: serviço/repositório com listagem, criação, renomeação e status;
- [x] categoria: migration `20261020113000_AddCategories` validada em apply/rollback/reapply;
- [x] categoria: domínio, autorização e persistência cobertos por testes;
- [x] categoria: catálogo WPF operacional em `Cadastros`, com ativos/inativos, criação, renomeação e status;
- [ ] categoria: não adicionar hierarquia, categoria-pai, código ou vínculo com Produto/Material sem requisito explícito;
- [x] centro de custo: entidade `CostCenter` global independente, com identidade, nome e status;
- [x] centro de custo: `cost-centers.read` e `cost-centers.manage` registrados no `PermissionCatalog`;
- [x] centro de custo: serviço/repositório com listagem, criação, renomeação e status;
- [x] centro de custo: migration `20261021100000_AddCostCenters` validada em apply/rollback/reapply;
- [x] centro de custo: domínio, autorização e persistência cobertos por testes;
- [x] centro de custo: catálogo WPF operacional em `Cadastros`, com ativos/inativos, criação, renomeação e status;
- [ ] centro de custo: não adicionar código, hierarquia, vínculo obrigatório com empresa/filial, regra financeira ou relacionamento automático sem requisito explícito.

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
- [x] `Categories` integrado ao snapshot EF e migration smoke;
- [x] `CostCenters` integrado ao snapshot EF e migration smoke;
- [x] `PurchaseRequests`/`PurchaseRequestLines` integrados ao snapshot EF, FKs e migration smoke;
- [x] RFQ, linhas, fornecedores convidados, respostas e linhas de resposta integrados ao snapshot EF e migration `20261021132811_AddRequestForQuotations`;
- [x] adjudicação RFQ integrada ao snapshot EF e migration `20261022105027_AddRequestForQuotationAwards`, com uma decisão por RFQ e FKs/unicidade validadas;
- [x] Pedido de Compra: integrar `PurchaseOrders`/`PurchaseOrderLines`, FKs/unicidades e reserva transacional de `SequenceNumber` ao EF Core;
- [x] Pedido de Compra: criar e validar a 17ª migration `20261022154117_AddPurchaseOrders` com round trip, concorrência e apply -> rollback 0 -> reapply;
- [ ] revisar o aviso conhecido do rollback de `AddAuthenticationThrottling` antes de release, sem reescrever histórico de migration de forma insegura.

## P1 — Primeiro módulo operacional

- [x] iniciar descoberta de Compras em `docs/PURCHASING.md`, comparando referências de mercado e separando padrões conceituais de regras internas não confirmadas;
- [x] resolver as doze decisões bloqueadoras do primeiro slice em `docs/PURCHASE-REQUEST-SPEC.md`;
- [x] documentar estados `Draft/Submitted/Cancelled`, atores, permissões `purchase-requests.read/manage`, invariantes, transições e limites de escopo;
- [x] implementar domínio `PurchaseRequest`, `PurchaseRequestLine` e `PurchaseRequestStatus` conforme a especificação;
- [x] adicionar permissões de Solicitação de Compra ao `PermissionCatalog` e bootstrap de instalações novas sem elevar roles existentes;
- [x] implementar contrato/repositório e casos de uso de criar/listar/consultar/editar rascunho/linhas/enviar/cancelar;
- [x] cobrir domínio e autorização com testes antes da UI;
- [x] garantir atomicidade das mutações de domínio em falhas de validação;
- [x] integrar EF Core, FKs, índices, precisão de quantidade e migration reproduzível;
- [x] validar snapshot e ciclo das 14 migrations em apply -> rollback 0 -> reapply;
- [x] implementar primeira UI WPF operacional da Solicitação de Compra;
- [x] lista WPF com filtro por status, loading/empty/error e criação de rascunho;
- [x] editor WPF de rascunho com observação e linhas, incluindo seleção de Unidade de Medida ativa;
- [x] implementar envio, cancelamento com justificativa e modo somente leitura para `Submitted`/`Cancelled`;
- [x] integrar navegação/DI do módulo Compras sem mover autorização para a UI;
- [x] preservar nome de Unidade de Medida inativa em leitura histórica, oferecendo somente unidades ativas para novas alterações;
- [x] especificar Cotação/RFQ em `docs/REQUEST-FOR-QUOTATION-SPEC.md` antes da implementação;
- [x] implementar Domain/Application de RFQ e respostas com autorização deny-by-default;
- [x] integrar persistência EF, preço unitário escalado, migration e testes de round trip/FKs;
- [x] integrar workspace WPF de RFQ ao módulo Compras;
- [x] especificar o terceiro slice de Comparação de Cotações em `docs/QUOTATION-COMPARISON-SPEC.md`;
- [x] implementar read model autorizado e determinístico de comparação sem nova persistência;
- [x] integrar janela WPF somente leitura de comparação ao RFQ;
- [x] especificar Adjudicação manual em `docs/RFQ-AWARD-SPEC.md`;
- [x] implementar Domain/Application e permissão `request-for-quotations.award`;
- [x] integrar persistência EF, 16ª migration e testes de round trip/FKs/unicidade;
- [x] integrar janela WPF de adjudicação ao RFQ `Closed`;
- [x] especificar Pedido de Compra em `docs/PURCHASE-ORDER-SPEC.md` antes de gerar documento a partir da adjudicação;
- [x] implementar Domain/Application do Pedido de Compra com snapshots, `Draft/Issued/Cancelled`, `purchase-orders.read/manage` e termos reconstruídos da adjudicação;
- [x] cobrir criação, imutabilidade, emissão, cancelamento, autorização e agrupamento por fornecedor com testes;
- [x] implementar persistência EF e sequência transacional do Pedido de Compra;
- [x] integrar workspace WPF do Pedido de Compra depois da persistência validada;
- [ ] manter recebimento, estoque, financeiro e fiscal fora do Pedido de Compra até requisitos explícitos.

## P2 — Engenharia

- [x] CI Windows permanente;
- [x] `has-pending-model-changes` no CI;
- [x] build da ferramenta de benchmark no CI;
- [ ] coverage quando a suíte tiver volume suficiente;
- [x] verificação automatizada de dependências vulneráveis diretas/transitivas com NuGet Audit e gate JSON no CI;
- [x] script de package/release para pacote portátil Windows de QA, self-contained, com migration bundle, hashes e smoke do binário publicado;
- [x] smoke automatizado mínimo de UI para bootstrap, autenticação, shell e navegação básica de Compras;
- [ ] ampliar automação de UI somente para fluxos cujo custo de manutenção se justificar.

## Baseline atual — 2026-10-23

Empresa, filial, fornecedor, material, produto, unidade de medida, categoria e centro de custo permanecem operacionais nos cadastros-base.

O primeiro slice de **Solicitação de Compra** está implementado de Domain a WPF. Ele usa `Draft`, `Submitted` e `Cancelled`, linha por descrição/quantidade/Unidade de Medida e permissões `purchase-requests.read/manage`. A tela de Compras lista e filtra solicitações, cria/abre rascunhos, edita observação e linhas, envia, cancela com justificativa e mantém `Submitted`/`Cancelled` em modo somente leitura. Unidades inativas continuam disponíveis para resolver nomes históricos, mas não podem ser selecionadas em novas alterações.

Baseline automatizado atual: **271/271 testes** (Domain 75, Application 130, Persistence 59, Infrastructure 7), **17 migrations**, build Release e benchmark tool com 0 warnings/erros, smoke WPF automatizado, snapshot EF sem mudanças pendentes e apply -> rollback 0 -> reapply validados no CI Windows. O CI também gera um pacote portátil `win-x64` self-contained com migration bundle, SHA-256, inicializador seguro de banco novo e smoke executado sobre o binário publicado. A auditoria NuGet falha diante de qualquer advisory direto ou transitivo conhecido e possui teste sintético do próprio caminho de rejeição.

O segundo slice de **Cotação/RFQ** também está implementado de Domain a WPF: origem em Solicitação `Submitted`, seleção de linhas, fornecedores convidados, envio, respostas em rascunho/finalizadas, preço unitário, data prometida, encerramento e cancelamento. Não existe seleção automática de vencedor nem geração de Pedido de Compra.

O terceiro slice de **Comparação de Cotações** está implementado como leitura: fornecedores convidados, status/moeda das respostas e ofertas finalizadas por linha são exibidos sem ranking, conversão cambial ou seleção automática.

O quarto slice de **Adjudicação manual** também está implementado de Domain a WPF: somente RFQ `Closed`, uma oferta `Finalized` por linha inteira, linhas opcionais sem adjudicação, permissão própria, persistência auditável e decisão imutável após finalização.

O quinto slice de **Pedido de Compra** está implementado de Domain a WPF: um fornecedor por pedido, origem obrigatória em adjudicação `Finalized`, snapshots imutáveis dos termos adjudicados, estados `Draft/Issued/Cancelled`, permissões próprias, `PurchaseOrders`/`PurchaseOrderLines`, preço unitário escalado no SQLite e numeração crescente reservada sob transação imediata. O workspace WPF lista e filtra pedidos, mostra numeração `PC-000001`, cria somente a partir de adjudicações elegíveis, preserva linhas/valores como somente leitura, permite observação em `Draft`, emissão interna e cancelamento com justificativa, mantendo o histórico de emissão mesmo após cancelamento.

A próxima prioridade é **executar o smoke visual/interativo em Windows real do fluxo completo de Compras**, incluindo Solicitação, RFQ, Comparação, Adjudicação e Pedido de Compra. Qualquer próximo slice funcional de Compras deve ser especificado antes de código; aprovação, recebimento, estoque, financeiro, fiscal e integrações com fornecedor permanecem fora do escopo até haver regras explícitas. O benchmark em hardware representativo continua como pendência de pré-release. Entre as pendências de engenharia que não exigem nova regra de negócio, a próxima prioridade provável é estabelecer **coverage automatizado com baseline útil**, sem transformar percentual isolado em meta artificial.
