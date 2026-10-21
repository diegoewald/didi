# Cotação / RFQ — especificação do segundo slice de Compras

Status: **especificado; implementação pendente**.

Esta especificação define o segundo slice vertical de Compras. O objetivo é evoluir do documento interno `PurchaseRequest` para um processo de solicitação de proposta comercial a fornecedores sem introduzir aprovação, comparação automática, seleção de vencedor ou Pedido de Compra antes de suas regras próprias existirem.

As decisões abaixo são decisões de produto deste ERP para um slice mínimo, reversível e local-first. Não representam processo interno de empregador, fornecedor ou organização real.

## 1. Por que RFQ é o próximo slice

O `PROJECT-BRIEF.md` prevê, dentro de Compras, Solicitação de Compra, Cotação, comparação de fornecedores e Pedido de Compra. O primeiro slice de Solicitação de Compra já está implementado de Domain a WPF.

A pesquisa de referência mostra um padrão recorrente:

- Microsoft Dynamics 365: um RFQ é enviado a um ou mais fornecedores; as respostas registram preço e prazo e podem ser comparadas posteriormente;
- Oracle Supplier Negotiations: RFQ é um documento enviado a um ou mais fornecedores para obter cotações;
- SAP: RFQ é um convite ao fornecedor para apresentar uma cotação para materiais ou serviços e pode ser criado com referência a uma requisição.

Essas referências justificam somente a separação conceitual `requisição -> RFQ -> resposta`. Elas não tornam obrigatórios três fornecedores, menor preço, aprovação, seleção automática ou Pedido de Compra.

Referências:

- https://learn.microsoft.com/en-us/dynamics365/supply-chain/procurement/request-quotations
- https://docs.oracle.com/en/cloud/saas/procurement/26d/oaprc/negotiation-types.html
- https://help.sap.com/docs/SAP_S4HANA_ON-PREMISE/af9ef57f504840d2b81be8667206d485/737eb65334e6b54ce10000000a174cb4.html

## 2. Limite do slice

O segundo slice deve permitir:

- criar um RFQ a partir de uma Solicitação de Compra `Submitted`;
- selecionar uma ou mais linhas da Solicitação de Compra como objeto do RFQ;
- convidar um ou mais fornecedores ativos;
- enviar o RFQ;
- registrar uma resposta de fornecedor em rascunho;
- informar preço unitário por linha e prazo/data prometida opcional;
- finalizar a resposta registrada;
- encerrar o RFQ quando houver ao menos uma resposta finalizada;
- cancelar um RFQ em rascunho ou enviado com justificativa;
- consultar RFQs, fornecedores convidados e respostas.

Este slice termina em `Closed`. Ele não escolhe vencedor e não cria Pedido de Compra.

## 3. Modelo conceitual

### 3.1 `RequestForQuotation`

Cabeçalho do processo de cotação.

Campos previstos:

- `Id` — `Guid` imutável;
- `PurchaseRequestId` — origem obrigatória;
- `Status` — `Draft`, `Sent`, `Closed` ou `Cancelled`;
- `CreatedByUserId`;
- `CreatedAtUtc`;
- `Notes` — opcional, até 1000 caracteres;
- `SentByUserId` / `SentAtUtc` quando enviado;
- `ClosedByUserId` / `ClosedAtUtc` quando encerrado;
- `CancelledByUserId` / `CancelledAtUtc` / `CancellationReason` quando cancelado.

### 3.2 `RequestForQuotationLine`

Cada linha referencia uma linha da Solicitação de Compra de origem.

Campos previstos:

- `Id` — `Guid`;
- `RequestForQuotationId`;
- `PurchaseRequestLineId`;
- `LineNumber` — inteiro positivo e único dentro do RFQ.

Descrição, quantidade, Unidade de Medida e data de necessidade continuam sendo obtidas da linha da Solicitação de Compra. O RFQ não altera esses dados no segundo slice.

Uma linha da Solicitação pode participar de mais de um RFQ no futuro; nenhuma unicidade global será criada entre `PurchaseRequestLineId` e RFQ. Dentro de um mesmo RFQ, a mesma linha de origem não pode ser adicionada duas vezes.

### 3.3 `RequestForQuotationSupplier`

Representa um fornecedor convidado.

Campos previstos:

- `Id` — `Guid`;
- `RequestForQuotationId`;
- `SupplierId`.

O par `(RequestForQuotationId, SupplierId)` deve ser único.

### 3.4 `SupplierQuotation`

Resposta registrada internamente para um fornecedor convidado.

Campos previstos:

- `Id` — `Guid`;
- `RequestForQuotationId`;
- `SupplierId`;
- `Status` — `Draft` ou `Finalized`;
- `CurrencyCode` — obrigatório, exatamente 3 letras ASCII, normalizado para maiúsculas;
- `SupplierReference` — opcional, até 100 caracteres;
- `ValidUntilDate` — opcional;
- `Notes` — opcional, até 1000 caracteres;
- `CreatedByUserId` / `CreatedAtUtc`;
- `FinalizedByUserId` / `FinalizedAtUtc` quando finalizada.

Existe no máximo uma `SupplierQuotation` por fornecedor dentro do mesmo RFQ neste slice. Revisões/versões de cotação ficam fora do escopo inicial.

### 3.5 `SupplierQuotationLine`

Campos previstos:

- `Id` — `Guid`;
- `SupplierQuotationId`;
- `RequestForQuotationLineId`;
- `UnitPrice` — decimal não negativo, com no máximo 6 casas decimais;
- `PromisedDeliveryDate` — opcional;
- `Notes` — opcional, até 500 caracteres.

A mesma linha do RFQ não pode aparecer duas vezes na mesma resposta.

A ausência de `SupplierQuotationLine` para uma linha significa apenas que aquela linha não foi cotada nessa resposta. Não será criada regra automática para interpretar isso como rejeição, indisponibilidade ou zero.

## 4. Estados e transições

### RFQ

- criação -> `Draft`;
- `Draft` -> `Sent`;
- `Draft` -> `Cancelled`;
- `Sent` -> `Closed`;
- `Sent` -> `Cancelled`.

`Closed` e `Cancelled` são terminais neste slice.

### Resposta do fornecedor

- criação -> `Draft`;
- `Draft` -> `Finalized`.

`Finalized` é imutável neste slice. Fluxo de revisão, substituição ou reabertura de resposta será especificado apenas quando houver requisito real.

## 5. Regras da origem

1. O RFQ deve referenciar exatamente uma `PurchaseRequest` existente.
2. A Solicitação de Compra precisa estar em `Submitted` no momento da criação do RFQ.
3. O RFQ deve conter ao menos uma linha da Solicitação de origem antes do envio.
4. Somente linhas pertencentes à Solicitação de origem podem ser adicionadas.
5. Cancelar posteriormente a Solicitação de Compra não cancela automaticamente RFQs já criados. Nenhuma cascata de status entre documentos será presumida.
6. A Application não cria RFQ a partir de Solicitação `Draft` ou `Cancelled`.

## 6. Regras de fornecedores

1. Somente fornecedores existentes e ativos podem ser convidados enquanto o RFQ está em `Draft`.
2. O RFQ precisa possuir ao menos um fornecedor antes do envio.
3. Um fornecedor não pode ser convidado duas vezes no mesmo RFQ.
4. Desativar um fornecedor após o envio não remove seu vínculo histórico nem invalida uma resposta já registrada.
5. Não existe quantidade mínima de fornecedores além de um.
6. Não existe fornecedor vencedor neste slice.

## 7. Edição por estado

### `Draft`

Pode editar:

- observação;
- linhas de origem;
- fornecedores convidados.

Não aceita respostas de fornecedor antes do envio.

### `Sent`

Cabeçalho, linhas e fornecedores ficam imutáveis.

Pode:

- criar/editar respostas `Draft` de fornecedores convidados;
- finalizar respostas;
- encerrar o RFQ;
- cancelar o RFQ.

### `Closed`

Somente leitura.

### `Cancelled`

Somente leitura.

## 8. Regras das respostas

1. Resposta só pode ser criada para fornecedor convidado no RFQ.
2. Resposta só pode ser criada/editada enquanto o RFQ estiver `Sent`.
3. `CurrencyCode` é obrigatório e não é inferido do locale da máquina.
4. O código é normalizado para maiúsculas e validado apenas estruturalmente como três letras ASCII neste slice; não haverá tabela online de moedas nem dependência de internet.
5. Uma resposta `Draft` pode conter zero ou mais linhas durante edição.
6. Para finalizar, deve existir ao menos uma linha cotada.
7. Cada linha cotada deve pertencer ao RFQ correspondente.
8. `UnitPrice` não pode ser negativo e não pode possuir mais de seis casas decimais.
9. Não existe conversão cambial automática.
10. Não existe cálculo de imposto, frete, desconto, condição de pagamento ou custo total comparável neste slice.
11. Uma resposta `Finalized` é imutável.

## 9. Política monetária

`UnitPrice` é valor monetário e deve respeitar o ADR `docs/adr/0005-monetary-values-sqlite.md`.

No Domain/Application será `decimal`. Na persistência SQLite, quando implementada, deve usar inteiro assinado de 64 bits com escala fixa de 6 casas e conversão `checked`. Nenhum caminho financeiro utilizará `double`.

Não existe arredondamento automático para centavos. Valores com mais de seis casas devem ser rejeitados, não silenciosamente arredondados.

## 10. Encerramento e cancelamento

Para enviar um RFQ:

- status deve ser `Draft`;
- deve possuir ao menos uma linha;
- deve possuir ao menos um fornecedor.

Para encerrar:

- status deve ser `Sent`;
- deve existir ao menos uma resposta `Finalized`.

Para cancelar:

- status deve ser `Draft` ou `Sent`;
- justificativa obrigatória, normalizada e limitada a 500 caracteres.

Não existe hard delete público do RFQ ou de respostas finalizadas.

## 11. Atores e permissões

Permissões do segundo slice:

- `request-for-quotations.read` — consultar RFQs e respostas;
- `request-for-quotations.manage` — criar/editar/enviar/encerrar/cancelar RFQ e registrar/finalizar respostas.

A autorização permanece na Application. Esconder ou desabilitar controles no WPF não substitui `DemandAsync`.

Operações que registram ator exigem sessão autenticada e usuário ativo.

Roles existentes não recebem novas permissões automaticamente; somente instalações novas recebem o catálogo atualizado pelo bootstrap já existente.

## 12. Invariantes principais

1. Identificadores obrigatórios não podem ser `Guid.Empty`.
2. Timestamps técnicos são UTC.
3. `Draft` é o único estado do RFQ que aceita mudança de linhas e fornecedores.
4. `Sent` bloqueia estrutura do RFQ.
5. `Closed` e `Cancelled` são terminais.
6. Fornecedor convidado deve ser único por RFQ.
7. Linha de origem deve ser única por RFQ.
8. Resposta deve pertencer a fornecedor convidado.
9. Há no máximo uma resposta por fornecedor/RFQ neste slice.
10. Linha de resposta deve apontar para linha do mesmo RFQ.
11. Resposta finalizada é imutável.
12. RFQ só pode fechar com ao menos uma resposta finalizada.
13. Validações devem ocorrer antes de alterar estado persistível para evitar mutações parciais em memória em caso de falha.

## 13. Casos de uso previstos na Application

- `CreateDraftAsync(purchaseRequestId, notes)`;
- `ListAsync(status?)`;
- `GetByIdAsync(id)`;
- `UpdateDraftAsync(id, notes)`;
- `AddSourceLineAsync(id, purchaseRequestLineId)`;
- `RemoveSourceLineAsync(id, rfqLineId)`;
- `AddSupplierAsync(id, supplierId)`;
- `RemoveSupplierAsync(id, supplierId)`;
- `SendAsync(id)`;
- `CreateSupplierQuotationAsync(id, supplierId, currencyCode, ...)`;
- `UpdateSupplierQuotationAsync(...)`;
- `UpsertSupplierQuotationLineAsync(...)`;
- `RemoveSupplierQuotationLineAsync(...)`;
- `FinalizeSupplierQuotationAsync(...)`;
- `CloseAsync(id)`;
- `CancelAsync(id, reason)`.

Os nomes definitivos podem variar durante implementação sem alterar as regras funcionais desta especificação.

## 14. Primeira UI prevista

Quando Domain/Application/Persistence estiverem verdes, a UI WPF deverá permitir:

- lista de RFQs com filtro por status;
- criação a partir de Solicitação `Submitted`;
- seleção das linhas da Solicitação;
- seleção de fornecedores ativos;
- envio;
- registro de resposta por fornecedor;
- preço unitário e data prometida por linha;
- finalização da resposta;
- encerramento e cancelamento;
- leitura histórica de fornecedor mesmo se desativado depois;
- loading/empty/error consistentes com o shell.

## 15. Fora do escopo

Não implementar neste slice:

- aprovação de Solicitação ou RFQ;
- alçada por valor;
- três cotações obrigatórias;
- seleção automática por menor preço;
- classificação de vencedor/perdedor;
- comparação automática entre moedas;
- câmbio;
- impostos;
- frete;
- descontos;
- condição de pagamento;
- anexos;
- portal do fornecedor;
- e-mail automático;
- revisão/versionamento de resposta finalizada;
- Pedido de Compra;
- recebimento;
- estoque;
- financeiro;
- fiscal.

## 16. Próxima implementação

A implementação deve começar por **Domain + Application**, com testes das invariantes e autorização. Persistência EF/migration e WPF entram somente depois de o núcleo estar verde.
