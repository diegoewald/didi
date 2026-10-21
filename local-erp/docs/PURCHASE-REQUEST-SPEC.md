# Solicitação de Compra — especificação do primeiro slice

Status: **núcleo e persistência implementados; UI WPF pendente**.

Esta especificação resolve as decisões mínimas necessárias para o primeiro slice vertical de Compras sem transformar práticas de uma empresa específica em regras universais do ERP.

As decisões abaixo são **decisões de produto deste ERP para o primeiro slice**. Elas foram escolhidas para manter o modelo pequeno, reversível e compatível com evolução posterior. Não representam processo interno de empregador, fornecedor ou organização real.

## 1. Objetivo do slice

Entregar uma Solicitação de Compra local-first capaz de:

- criar uma solicitação em rascunho;
- registrar uma ou mais linhas;
- editar o rascunho;
- remover linhas do rascunho;
- enviar a solicitação para o processo de Compras;
- cancelar um rascunho ou uma solicitação já enviada;
- consultar solicitações e seu estado.

Este slice termina em `Submitted`. Ele **não** implementa aprovação, cotação, comparação de fornecedores, pedido de compra, recebimento, estoque, financeiro ou fiscal.

## 2. Decisões funcionais resolvidas

### 2.1 O que pode ser solicitado

A linha é baseada em **descrição livre obrigatória**.

No primeiro slice não existe vínculo obrigatório com `Material`, `Product`, `Supplier` ou outro cadastro comercial. Isso permite registrar uma necessidade sem presumir a natureza contábil, fiscal ou logística do item.

Um vínculo opcional com `Material` poderá ser acrescentado em slice posterior quando houver requisito operacional claro. `Product` não será tratado como sinônimo de item comprado.

### 2.2 Vínculos organizacionais

Nenhum vínculo com `Company`, `CompanyBranch` ou `CostCenter` é obrigatório no primeiro slice.

Esses campos também não foram adicionados apenas como opcionais nesta primeira entidade. Quando houver regra real sobre qual vínculo é necessário, a relação será introduzida por migration explícita e com invariantes correspondentes.

### 2.3 Dados mínimos da solicitação

Cabeçalho:

- `Id` — `Guid` imutável;
- `Status`;
- `RequesterUserId` — usuário autenticado que criou a solicitação;
- `CreatedAtUtc`;
- `Notes` — opcional, até 1000 caracteres;
- dados de envio/cancelamento quando aplicáveis.

Linha:

- `Id` — `Guid` imutável;
- `PurchaseRequestId`;
- `LineNumber` — inteiro positivo e único dentro da solicitação;
- `Description` — obrigatório, normalizado e até 500 caracteres;
- `Quantity` — decimal maior que zero;
- `MeasurementUnitId` — obrigatório;
- `NeededByDate` — opcional.

Não existem no primeiro slice preço, moeda, fornecedor, imposto, frete, condição de pagamento ou valor estimado.

### 2.4 Estados

O primeiro slice possui somente:

- `Draft`;
- `Submitted`;
- `Cancelled`.

Transições permitidas:

- criação -> `Draft`;
- `Draft` -> `Submitted`;
- `Draft` -> `Cancelled`;
- `Submitted` -> `Cancelled`.

Não existem `Approved`, `Rejected`, `Quoted`, `Ordered`, `Received` ou estados equivalentes neste slice.

### 2.5 Atores e permissões

O criador é sempre o usuário da sessão autenticada.

Permissões iniciais:

- `purchase-requests.read` — consultar solicitações;
- `purchase-requests.manage` — criar, editar rascunhos, adicionar/remover/alterar linhas, enviar e cancelar.

A autorização permanece na camada Application. Esconder ou desabilitar controles na UI não substitui autorização.

Neste primeiro slice não existe restrição de visibilidade ou edição baseada em propriedade do registro. Um usuário com `purchase-requests.manage` pode operar qualquer solicitação dentro das transições permitidas. Se futuramente houver escopo por solicitante, empresa, filial ou departamento, será uma regra nova e explícita.

Instalações novas recebem essas permissões pelo mesmo mecanismo de bootstrap baseado no `PermissionCatalog`. Roles já existentes não recebem elevação automática de privilégio.

### 2.6 Aprovação

Não existe aprovação no primeiro slice.

`Submitted` significa apenas que o rascunho foi concluído e entregue ao processo de Compras. Não significa aprovação, autorização financeira, escolha de fornecedor nem autorização de pedido.

Aprovação será um slice separado para que regras de aprovador, alçada, retorno, rejeição e reabertura não contaminem o núcleo da Solicitação de Compra.

### 2.7 Cotação

Cotação não é requisito para enviar a Solicitação de Compra neste slice.

Isso não significa que cotação será opcional no produto final. Significa apenas que Solicitação de Compra não depende de uma entidade de Cotação que ainda não existe.

### 2.8 Relação com Pedido de Compra

Não existe FK ou conversão para Pedido de Compra neste slice.

O modelo não presume se uma solicitação gerará um pedido, vários pedidos, ou se várias solicitações poderão ser consolidadas. Essa cardinalidade será definida quando o slice de Pedido de Compra for especificado.

### 2.9 Recebimento parcial

Fora do escopo. Nenhuma coluna de quantidade recebida, saldo, encerramento ou recebimento foi adicionada à Solicitação de Compra.

### 2.10 Numeração

O identificador persistente do primeiro slice é `Guid`.

Não será criada sequência humana permanente antes de existir regra aprovada para escopo e formato da numeração. A UI pode localizar registros por dados de contexto e pelo identificador técnico, mas não deve transformar um trecho do `Guid` em número oficial do documento.

Uma numeração amigável deverá ser especificada antes de documentos externos como Pedido de Compra.

### 2.11 Alterações permitidas

`Draft`:

- cabeçalho editável;
- linhas podem ser incluídas, alteradas e removidas;
- pode ser enviado;
- pode ser cancelado.

`Submitted`:

- conteúdo imutável;
- pode somente ser consultado ou cancelado.

`Cancelled`:

- conteúdo imutável;
- nenhuma reabertura no primeiro slice.

Não existe exclusão física de Solicitação de Compra pela Application. Cancelamento preserva rastreabilidade.

### 2.12 Histórico funcional mínimo

O primeiro slice não cria uma tabela genérica de eventos funcionais.

O ciclo mínimo é preservado no próprio documento:

- `CreatedAtUtc` e `RequesterUserId`;
- `SubmittedAtUtc` e `SubmittedByUserId`;
- `CancelledAtUtc` e `CancelledByUserId`;
- `CancellationReason` obrigatório no cancelamento, normalizado e até 500 caracteres.

Logs estruturados podem registrar as operações, mas log não substitui estado persistido. Uma timeline funcional genérica será avaliada quando existirem mais documentos e transições no módulo.

## 3. Invariantes do domínio

1. Nova solicitação sempre nasce em `Draft`.
2. `RequesterUserId` não pode ser vazio.
3. `Notes` é opcional, normalizada e limitada a 1000 caracteres.
4. Somente `Draft` aceita alteração de cabeçalho ou linhas.
5. Uma linha exige descrição não vazia após `Trim`, até 500 caracteres.
6. `Quantity` deve ser maior que zero.
7. `MeasurementUnitId` não pode ser vazio.
8. `LineNumber` deve ser positivo e único dentro da solicitação.
9. Para enviar, a solicitação precisa possuir pelo menos uma linha válida.
10. Ao adicionar ou alterar linha, a Application verifica que a Unidade de Medida existe e está ativa.
11. `Submitted` não pode voltar para `Draft`.
12. `Cancelled` é terminal.
13. Cancelamento exige justificativa não vazia de até 500 caracteres.
14. Datas de criação, envio e cancelamento são persistidas em UTC usando `DateTime`, alinhado ao `IClock` e ao padrão técnico existente do ERP.
15. `NeededByDate` é opcional e não possui regra automática que proíba datas passadas neste slice.
16. Não existe hard delete exposto pela Application.
17. Validações de mutação são executadas antes de alterar estado persistível, evitando alterações parciais em memória quando uma transição falha.

## 4. Modelo implementado

### `PurchaseRequest`

- `Guid Id`
- `PurchaseRequestStatus Status`
- `Guid RequesterUserId`
- `DateTime CreatedAtUtc`
- `string? Notes`
- `DateTime? SubmittedAtUtc`
- `Guid? SubmittedByUserId`
- `DateTime? CancelledAtUtc`
- `Guid? CancelledByUserId`
- `string? CancellationReason`
- coleção de `PurchaseRequestLine`

### `PurchaseRequestLine`

- `Guid Id`
- `Guid PurchaseRequestId`
- `int LineNumber`
- `string Description`
- `decimal Quantity`
- `Guid MeasurementUnitId`
- `DateOnly? NeededByDate`

`Quantity` usa precisão EF `18,4`; não é armazenada como `double`/`float`.

## 5. Persistência implementada

Tabelas:

- `PurchaseRequests`;
- `PurchaseRequestLines`.

Relações:

- `PurchaseRequest.RequesterUserId -> Users.Id` com `RESTRICT`;
- `SubmittedByUserId -> Users.Id` com `RESTRICT` quando preenchido;
- `CancelledByUserId -> Users.Id` com `RESTRICT` quando preenchido;
- `PurchaseRequestLine.PurchaseRequestId -> PurchaseRequests.Id` com `CASCADE` apenas para o filho interno do agregado;
- `PurchaseRequestLine.MeasurementUnitId -> MeasurementUnits.Id` com `RESTRICT`.

Índices:

- status da solicitação;
- data de criação;
- solicitante;
- atores de envio/cancelamento;
- unidade de medida das linhas;
- unicidade de `(PurchaseRequestId, LineNumber)`.

O repositório EF carrega o agregado com linhas e sincroniza inclusão, alteração e remoção dos filhos em um único `SaveChangesAsync`.

A migration `20261021123000_AddPurchaseRequests` é a 14ª migration do projeto.

## 6. Casos de uso implementados na Application

- `CreateDraftAsync`;
- `ListAsync`;
- `GetByIdAsync`;
- `UpdateDraftAsync`;
- `AddLineAsync`;
- `UpdateLineAsync`;
- `RemoveLineAsync`;
- `SubmitAsync`;
- `CancelAsync`.

Consultas exigem `purchase-requests.read`; mutações exigem `purchase-requests.manage`. Operações que registram ator exigem sessão autenticada e usuário ativo.

A Application valida Unidade de Medida existente e ativa antes de inserir ou alterar linha.

## 7. Concorrência e consistência

O produto atual é single-PC/local-first, porém a modelagem não depende conceitualmente de ausência eterna de concorrência.

O primeiro slice evita operações de regra de negócio espalhadas na UI. Mutações carregam o agregado, executam suas invariantes e o repositório persiste cabeçalho e linhas conjuntamente.

Controle de concorrência otimista/versionamento de linha não foi introduzido sem necessidade comprovada no cenário single-PC atual; deverá ser reavaliado na preparação multiusuário.

## 8. Primeira UI WPF prevista

A UI operacional ainda pendente deve permitir:

- lista de solicitações;
- filtro básico por status;
- criar rascunho;
- abrir rascunho;
- editar observação;
- adicionar/editar/remover linhas;
- selecionar Unidade de Medida ativa;
- enviar;
- cancelar com confirmação e justificativa;
- visualizar solicitação enviada/cancelada em modo somente leitura;
- estados loading/empty/error consistentes com o shell.

A UI não implementará aprovação, cotação, fornecedor, preço ou recebimento.

## 9. Validação automatizada do núcleo e persistência

O CI #143 validou o HEAD funcional de persistência com:

- build Release sem warnings/erros;
- 216/216 testes: Domain 56, Application 105, Persistence 49, Infrastructure 6;
- `has-pending-model-changes` sem diferenças;
- 14 migrations em apply -> rollback 0 -> reapply;
- testes de round-trip, sincronização de linhas e FKs do novo agregado.

O aviso histórico do rollback de `AddAuthenticationThrottling` sobre `PRAGMA foreign_keys = 0` continua não bloqueante e não foi introduzido por Compras.

## 10. Fora do escopo do primeiro slice

Não implementar neste momento:

- aprovação e rejeição;
- retorno para correção ou reabertura;
- cotação;
- fornecedores na solicitação;
- preço estimado ou histórico de preço;
- pedido de compra;
- recebimento;
- anexos;
- estoque;
- financeiro;
- fiscal;
- impostos;
- moeda;
- frete;
- condição de pagamento;
- Centro de Custo obrigatório;
- Empresa/Filial obrigatória;
- integração automática com Material/Produto;
- numeração sequencial oficial;
- notificações;
- workflow por linha;
- aprovação por alçada;
- consolidação/divisão de solicitações em pedidos.

## 11. Próxima prioridade técnica

O domínio, a Application, a persistência EF, a migration e os testes do primeiro slice estão concluídos e verdes.

A próxima prioridade é implementar a **primeira UI WPF operacional de Solicitação de Compra**, consumindo exclusivamente os casos de uso existentes e mantendo todas as regras/autorização na Application/Domain.
