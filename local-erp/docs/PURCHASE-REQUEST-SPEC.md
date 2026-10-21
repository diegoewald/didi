# Solicitação de Compra — especificação do primeiro slice

Status: **pronto para implementação**.

Esta especificação resolve as decisões mínimas necessárias para iniciar o primeiro slice vertical de Compras sem transformar práticas de uma empresa específica em regras universais do ERP.

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

Esses campos também não serão adicionados apenas como opcionais nesta primeira entidade. Quando houver regra real sobre qual vínculo é necessário, a relação será introduzida por migration explícita e com invariantes correspondentes.

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

Instalações novas podem receber essas permissões pelo mesmo mecanismo de bootstrap usado pelos demais módulos. Roles já existentes não recebem elevação automática de privilégio.

### 2.6 Aprovação

Não existe aprovação no primeiro slice.

`Submitted` significa apenas que o rascunho foi concluído e entregue ao processo de Compras. Não significa aprovação, autorização financeira, escolha de fornecedor nem autorização de pedido.

Aprovação será um slice separado para que regras de aprovador, alçada, retorno, rejeição e reabertura não contaminem o núcleo da Solicitação de Compra.

### 2.7 Cotação

Cotação não é requisito para enviar a Solicitação de Compra neste slice.

Isso não significa que cotação será opcional no produto final. Significa apenas que Solicitação de Compra não dependerá de uma entidade de Cotação que ainda não existe.

### 2.8 Relação com Pedido de Compra

Não existe FK ou conversão para Pedido de Compra neste slice.

O modelo não presume se uma solicitação gerará um pedido, vários pedidos, ou se várias solicitações poderão ser consolidadas. Essa cardinalidade será definida quando o slice de Pedido de Compra for especificado.

### 2.9 Recebimento parcial

Fora do escopo. Nenhuma coluna de quantidade recebida, saldo, encerramento ou recebimento será adicionada à Solicitação de Compra.

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
10. Ao adicionar ou alterar linha, a Application deve verificar que a Unidade de Medida existe e está ativa.
11. `Submitted` não pode voltar para `Draft`.
12. `Cancelled` é terminal.
13. Cancelamento exige justificativa não vazia de até 500 caracteres.
14. Datas de criação, envio e cancelamento são persistidas em UTC.
15. `NeededByDate` é opcional e não terá, neste slice, regra automática que proíba datas passadas; tal política não será presumida.
16. Não existe hard delete exposto pela Application.

## 4. Modelo proposto

### `PurchaseRequest`

- `Guid Id`
- `PurchaseRequestStatus Status`
- `Guid RequesterUserId`
- `DateTimeOffset CreatedAtUtc`
- `string? Notes`
- `DateTimeOffset? SubmittedAtUtc`
- `Guid? SubmittedByUserId`
- `DateTimeOffset? CancelledAtUtc`
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

A precisão de `Quantity` deve ser explicitada na configuração EF, com valor adequado para quantidade e sem uso de `double`/`float`.

## 5. Persistência proposta

Tabelas:

- `PurchaseRequests`;
- `PurchaseRequestLines`.

Relações previstas:

- `PurchaseRequest.RequesterUserId -> Users.Id` com `RESTRICT`;
- `SubmittedByUserId -> Users.Id` com `RESTRICT` quando preenchido;
- `CancelledByUserId -> Users.Id` com `RESTRICT` quando preenchido;
- `PurchaseRequestLine.PurchaseRequestId -> PurchaseRequests.Id`;
- `PurchaseRequestLine.MeasurementUnitId -> MeasurementUnits.Id` com `RESTRICT`.

Índices mínimos:

- status da solicitação;
- data de criação;
- solicitante;
- linhas por `PurchaseRequestId`;
- unicidade de `(PurchaseRequestId, LineNumber)`.

A estratégia exata de `ON DELETE` para linhas deve manter coerência com a ausência de hard delete público e ser validada na migration/smoke test.

## 6. Casos de uso da Application

Primeiro conjunto:

- `CreateDraftAsync`;
- `ListAsync`;
- `GetByIdAsync`;
- `UpdateDraftAsync`;
- `AddLineAsync`;
- `UpdateLineAsync`;
- `RemoveLineAsync`;
- `SubmitAsync`;
- `CancelAsync`.

Todos devem exigir sessão autenticada e a permissão correspondente.

Mutações devem carregar o agregado, executar a transição no domínio e persistir de forma atômica.

## 7. Concorrência e consistência

O produto atual é single-PC/local-first, porém a modelagem não deve depender de ausência eterna de concorrência.

O primeiro slice deve evitar operações do tipo read-modify-write espalhadas entre UI e repositório. A Application deve executar mutações como unidade transacional.

Se os padrões atuais de persistência já utilizarem mecanismo de compare-and-set ou transação imediata para mutações concorrentes críticas, o novo módulo deve reutilizar a estratégia adequada em vez de criar um padrão paralelo.

## 8. Primeira UI WPF prevista

A primeira UI operacional deve permitir:

- lista de solicitações;
- filtro básico por status;
- criar rascunho;
- abrir rascunho;
- editar observação;
- adicionar/editar/remover linhas;
- enviar;
- cancelar com confirmação e justificativa;
- visualizar solicitação enviada/cancelada em modo somente leitura;
- estados loading/empty/error consistentes com o shell.

A UI não implementará aprovação, cotação, fornecedor, preço ou recebimento.

## 9. Testes obrigatórios antes da UI

Domínio:

- criação em `Draft`;
- normalização e limites;
- quantidade inválida;
- unidade vazia;
- numeração de linha inválida/duplicada;
- envio sem linhas bloqueado;
- envio válido;
- edição após envio bloqueada;
- cancelamento com e sem justificativa;
- estado cancelado terminal.

Application:

- `read` exigido para consulta;
- `manage` exigido para mutações;
- usuário da sessão registrado como solicitante/ator;
- unidade inexistente/inativa rejeitada;
- transições inválidas não persistem alteração parcial.

Persistência:

- round-trip do agregado e linhas;
- FKs reais;
- índice/constraint de linha única;
- migration apply/rollback/reapply;
- snapshot EF sem alterações pendentes.

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

## 11. Critério de conclusão da especificação

As doze decisões bloqueadoras de `docs/PURCHASING.md` estão resolvidas **para o primeiro slice** por esta especificação.

Isso não encerra a descoberta de todo o módulo de Compras; apenas libera a implementação de Solicitação de Compra em seu menor fluxo seguro.

Próxima prioridade técnica:

1. criar domínio `PurchaseRequest`/`PurchaseRequestLine` e `PurchaseRequestStatus`;
2. adicionar permissões `purchase-requests.read/manage`;
3. criar contratos Application/repositório;
4. cobrir domínio e autorização com testes;
5. somente depois integrar EF/migration e UI.
