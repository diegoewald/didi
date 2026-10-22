# Pedido de Compra — especificação do quinto slice de Compras

Status: **Domain, Application e persistência implementados e validados; UI WPF pendente**.

Este documento define o quinto slice vertical de Compras. O objetivo é transformar uma decisão de adjudicação `Finalized` em um documento de compra próprio, rastreável e local-first, sem antecipar aprovação, recebimento, estoque, financeiro, fiscal ou integrações com fornecedor.

As regras abaixo são decisões de produto mínimas e reversíveis para este ERP. Não representam processo interno de empregador, fornecedor ou organização real.

## 1. Motivação e referências

A cadeia já implementada é:

`Solicitação de Compra -> RFQ -> respostas -> comparação -> adjudicação`.

O `PROJECT-BRIEF.md` prevê Pedido de Compra depois da comparação de fornecedores e antes de acompanhamento/recebimento.

Referências oficiais reforçam a separação entre decisão de sourcing e documento de compra:

- Microsoft Dynamics 365 trata RFQ do tipo Purchase Order como fonte para geração do pedido quando uma proposta/linha é aceita;
- Oracle permite concluir a adjudicação sem criar documento de compra, criar o documento depois e agrupar linhas adjudicadas em documentos de compra antes da submissão;
- SAP define Purchase Order como instrução formal a um fornecedor para fornecer determinada quantidade de bens/serviços e estrutura o pedido em cabeçalho + itens;
- Oracle trata alteração e cancelamento como parte do ciclo de vida posterior do próprio pedido.

Referências:

- https://learn.microsoft.com/en-us/dynamics365/supply-chain/procurement/request-quotations
- https://docs.oracle.com/en/cloud/saas/procurement/25c/oaprc/how-you-complete-your-award.html
- https://docs.oracle.com/en/cloud/saas/procurement/25c/faspp/create-purchasing-documents.html
- https://docs.oracle.com/en/cloud/saas/procurement/26d/oaprc/purchase-order-life-cycle.html
- https://help.sap.com/docs/PRODUCT_ID/af9ef57f504840d2b81be8667206d485/a07eb65334e6b54ce10000000a174cb4.html

Essas referências sustentam `adjudicação -> pedido -> ciclo de execução`. Elas não tornam obrigatórios aprovação, condição de pagamento, frete, imposto, endereço de entrega, recebimento parcial ou integração eletrônica com fornecedor.

## 2. Limite do slice

O quinto slice deve permitir:

- criar um Pedido de Compra a partir de uma decisão de adjudicação `Finalized`;
- criar um pedido para **um único fornecedor** por vez;
- incluir automaticamente todas as linhas adjudicadas àquele fornecedor dentro da mesma decisão;
- manter no máximo um pedido por combinação `(RequestForQuotationAwardId, SupplierId)` nesta primeira versão;
- revisar o pedido em `Draft`;
- editar somente observação de cabeçalho em `Draft`;
- emitir o pedido;
- cancelar pedido `Draft` ou `Issued` com justificativa;
- consultar pedidos e suas linhas posteriormente;
- preservar os termos comerciais adjudicados como snapshot do pedido.

Este slice **não implementa recebimento** e **não envia o pedido ao fornecedor por e-mail/API/portal**.

## 3. Um fornecedor por pedido

Cada `PurchaseOrder` pertence a exatamente um `Supplier`.

Se uma adjudicação tiver linhas escolhidas de dois fornecedores, o sistema deve gerar dois pedidos distintos, um para cada fornecedor.

Nesta primeira versão:

- linhas de fornecedores diferentes nunca são misturadas no mesmo pedido;
- linhas do mesmo fornecedor e da mesma adjudicação são agrupadas em um único pedido;
- não consolidar linhas de adjudicações diferentes em um mesmo pedido;
- não dividir as linhas do mesmo fornecedor em múltiplos pedidos.

Motivo: fornecedor é a fronteira mínima natural do documento e o modelo atual não possui empresa compradora, filial compradora, local de entrega, condição comercial ou outra dimensão segura para agrupamento. Oracle também trata a criação de documentos como uma etapa de agrupamento das linhas adjudicadas; este ERP usa somente os dados que já existem para formar o primeiro agrupamento determinístico.

## 4. Origem obrigatória

O Pedido de Compra deste slice só pode nascer de `RequestForQuotationAwardStatus.Finalized`.

Não criar PO diretamente de:

- Solicitação de Compra;
- RFQ sem adjudicação;
- decisão de adjudicação `Draft`;
- fornecedor/linha digitados manualmente.

A decisão deve conter ao menos uma linha adjudicada ao fornecedor escolhido.

## 5. Fornecedor ativo

Para criar e emitir um novo Pedido de Compra, o fornecedor deve existir e estar ativo.

Se o fornecedor for desativado depois de o pedido ser emitido:

- o pedido continua legível e preservado;
- nenhuma informação histórica é removida;
- o status do pedido não muda automaticamente.

Se o fornecedor for desativado entre a criação do `Draft` e a emissão, `Issue` deve ser bloqueado até que o cadastro volte a permitir novas operações.

Esta é uma salvaguarda de produto para evitar nova obrigação comercial contra cadastro marcado como inativo; não altera documentos já emitidos.

## 6. Estados

`PurchaseOrderStatus` terá:

- `Draft` — documento criado para revisão interna;
- `Issued` — documento formalizado pelo sistema e imutável neste slice, exceto cancelamento;
- `Cancelled` — documento cancelado com motivo e preservado historicamente.

Transições permitidas:

- criação -> `Draft`;
- `Draft -> Issued`;
- `Draft -> Cancelled`;
- `Issued -> Cancelled`.

Não permitir:

- reabrir `Issued`;
- reabrir `Cancelled`;
- `Cancelled -> Issued`;
- hard delete público.

Alteração/amendment de pedido emitido será um slice futuro se houver requisito explícito.

## 7. Numeração

Cada pedido recebe um `SequenceNumber` inteiro positivo, único na instalação local.

Regras:

- sequência crescente;
- atribuição transacional na criação;
- gaps são permitidos e não têm significado fiscal;
- a sequência é uma referência interna do ERP, não número fiscal/legal;
- a UI pode formatar como `PC-000001`, mas o valor persistido é o inteiro.

A geração não deve usar `MAX + 1` sem proteção concorrente. A implementação deverá usar mecanismo transacional seguro para SQLite e permanecer migrável para PostgreSQL.

## 8. Cabeçalho

`PurchaseOrder` contém, no mínimo:

- `Id`;
- `SequenceNumber`;
- `RequestForQuotationAwardId`;
- `SupplierId`;
- `SupplierNameSnapshot`;
- `CurrencyCode`;
- `Status`;
- `CreatedByUserId`;
- `CreatedAtUtc`;
- `Notes?`;
- `IssuedByUserId?`;
- `IssuedAtUtc?`;
- `CancelledByUserId?`;
- `CancelledAtUtc?`;
- `CancellationReason?`;
- coleção de linhas.

`SupplierNameSnapshot` existe porque o nome do cadastro pode ser renomeado depois. O vínculo por `SupplierId` continua sendo preservado.

`CurrencyCode` é única por pedido porque todas as linhas de um fornecedor dentro de um RFQ vêm da mesma `SupplierQuotation`.

## 9. Linhas

Cada `PurchaseOrderLine` contém, no mínimo:

- `Id`;
- `PurchaseOrderId`;
- `LineNumber`;
- `RequestForQuotationAwardLineId`;
- `DescriptionSnapshot`;
- `Quantity`;
- `MeasurementUnitId`;
- `MeasurementUnitNameSnapshot`;
- `UnitPrice`;
- `PromisedDeliveryDate?`.

As linhas são montadas automaticamente a partir das seleções da adjudicação para o fornecedor do pedido.

Não permitir neste slice:

- linha manual;
- remover uma linha adjudicada do pedido;
- alterar quantidade;
- alterar preço;
- alterar moeda;
- trocar fornecedor;
- alterar data prometida copiada da resposta.

Se um termo comercial precisar mudar depois da adjudicação, isso exige uma futura regra explícita de revisão/amendment em vez de edição silenciosa do documento originado.

## 10. Snapshots e rastreabilidade

No momento da criação, o PO copia os termos necessários para permanecer legível como documento próprio:

- nome do fornecedor;
- descrição da linha;
- quantidade;
- nome da unidade;
- preço unitário;
- moeda do cabeçalho;
- data prometida opcional.

A origem continua rastreável por `RequestForQuotationAwardId` e `RequestForQuotationAwardLineId`.

Não duplicar dados fiscais, endereço, condição de pagamento ou dados bancários porque esses conceitos ainda não existem no modelo com regras definidas.

## 11. Valores monetários e totais

`UnitPrice` permanece `decimal` no Domain/Application e deve seguir ADR-0005 no SQLite, com inteiro escalado de 6 casas.

Para cada linha:

`LineTotal = Quantity * UnitPrice`

O total do pedido é a soma dos `LineTotal`.

Regras:

- cálculo em `decimal`;
- nunca usar `double`;
- total é derivado, não precisa ser persistido nesta primeira versão;
- não somar valores de moedas diferentes, o que é evitado por uma única moeda por pedido;
- não existem impostos, frete, desconto, encargos ou câmbio neste slice.

## 12. Criação

Caso de uso sugerido:

`CreateDraftFromAwardAsync(requestForQuotationAwardId, supplierId, notes?)`

A Application deve:

1. exigir `purchase-orders.manage`;
2. validar identificadores;
3. carregar a adjudicação e exigir `Finalized`;
4. carregar o RFQ relacionado e as respostas finalizadas;
5. carregar o fornecedor e exigir ativo;
6. encontrar todas as linhas da adjudicação pertencentes ao fornecedor informado;
7. exigir ao menos uma linha;
8. rejeitar se já existir PO para `(AwardId, SupplierId)`;
9. resolver description/quantity/unidade da origem e preço/moeda/data da resposta escolhida;
10. validar que todas as linhas encontradas possuem a mesma moeda;
11. gerar `SequenceNumber` de forma transacional;
12. criar e persistir o agregado completo atomicamente.

Não confiar em preço, fornecedor, moeda ou quantidade enviados pela UI.

## 13. Edição de rascunho

Em `Draft`, apenas `Notes` pode ser alterado neste slice.

`Notes`:

- opcional;
- normalizada;
- máximo de 1000 caracteres.

As linhas já representam a adjudicação finalizada e não são um editor comercial.

## 14. Emissão

`Issue` exige:

- status `Draft`;
- pelo menos uma linha;
- fornecedor ainda ativo;
- usuário autenticado e ativo;
- timestamp UTC.

Ao emitir:

- definir `IssuedByUserId`;
- definir `IssuedAtUtc`;
- status -> `Issued`.

`Issued` significa **formalizado dentro do ERP**. Não significa que houve transmissão eletrônica, aceite do fornecedor ou recebimento.

## 15. Cancelamento

`Cancel` é permitido em `Draft` ou `Issued`.

Exige:

- usuário autenticado e ativo;
- timestamp UTC;
- motivo obrigatório, normalizado, máximo de 500 caracteres.

Ao cancelar:

- definir `CancelledByUserId`;
- definir `CancelledAtUtc`;
- definir `CancellationReason`;
- status -> `Cancelled`.

Não apagar o pedido nem suas linhas.

## 16. Autorização

Adicionar:

- `purchase-orders.read` — listar/consultar pedidos;
- `purchase-orders.manage` — criar rascunho, editar observação, emitir e cancelar.

As permissões entram no `PermissionCatalog` e no bootstrap de instalações novas. Roles existentes não recebem grants automaticamente.

Não reutilizar `request-for-quotations.award`: decidir o fornecedor e administrar o documento de compra são capacidades diferentes.

## 17. Persistência prevista

Adicionar:

### `PurchaseOrders`

- PK `Id`;
- `SequenceNumber` único;
- FK `RequestForQuotationAwardId -> RequestForQuotationAwards` com `RESTRICT`;
- FK `SupplierId -> Suppliers` com `RESTRICT`;
- FKs de usuários de criação/emissão/cancelamento com `RESTRICT`;
- unicidade `(RequestForQuotationAwardId, SupplierId)`;
- índices por status, fornecedor e data de criação.

### `PurchaseOrderLines`

- PK `Id`;
- FK `PurchaseOrderId -> PurchaseOrders` com `CASCADE` somente dentro do agregado;
- FK `RequestForQuotationAwardLineId -> RequestForQuotationAwardLines` com `RESTRICT`;
- FK `MeasurementUnitId -> MeasurementUnits` com `RESTRICT`;
- unicidade `(PurchaseOrderId, LineNumber)`;
- unicidade de `RequestForQuotationAwardLineId` neste primeiro slice, impedindo a mesma linha adjudicada de gerar dois pedidos.

### sequência

A implementação deve persistir/gerar o próximo `SequenceNumber` de modo transacional e testável. O desenho concreto deve evitar dependência de API específica do SQLite dentro do Domain/Application.

## 18. UI prevista

A primeira UI deve permitir:

- listar pedidos com filtro por status;
- exibir número interno, fornecedor, moeda, total derivado, data e status;
- abrir pedido;
- a partir de adjudicação `Finalized`, visualizar fornecedores com linhas ainda não convertidas em PO;
- criar o rascunho de um fornecedor;
- revisar linhas e totais;
- editar observação em `Draft`;
- emitir;
- cancelar com justificativa;
- exibir `Issued`/`Cancelled` como somente leitura;
- deixar explícito que emissão não equivale a envio eletrônico ou recebimento.

A UI não calcula nem decide termos comerciais por conta própria; usa resultados da Application.

## 19. Fora do escopo

Não implementar neste slice:

- Pedido de Compra manual sem adjudicação;
- consolidação entre adjudicações diferentes;
- split de uma linha adjudicada;
- alteração de quantidade/preço/moeda após adjudicação;
- aprovação/alçada/release strategy;
- assinatura;
- envio por e-mail/API/EDI/portal;
- confirmação/aceite do fornecedor;
- endereço de entrega;
- empresa/filial compradora obrigatória;
- condição de pagamento;
- incoterms;
- impostos;
- frete;
- desconto/encargos;
- anexos;
- serviços especiais/subcontratação/consignação;
- amendment/revisão de pedido emitido;
- recebimento;
- estoque;
- invoice matching/financeiro/fiscal.

## 20. Critério de conclusão

O slice estará concluído quando:

1. Domain cobrir estados, imutabilidade, observação, emissão e cancelamento;
2. Application montar o PO somente de adjudicação `Finalized`, agrupar todas as linhas do fornecedor e não confiar em termos comerciais da UI;
3. geração do `SequenceNumber` for transacional e segura contra concorrência;
4. banco garantir uma PO por `(AwardId, SupplierId)` e uma conversão por linha adjudicada;
5. persistência preservar snapshots e preço exato;
6. WPF permitir criação/revisão/emissão/cancelamento sem lógica de negócio própria;
7. build/testes/snapshot/migrations permanecerem verdes no CI.

## 21. Próxima etapa

Somente depois de Pedido de Compra estar operacional deverá ser especificado o próximo slice de **acompanhamento/recebimento**. Essa etapa precisará decidir recebimento total/parcial, saldo aberto, data efetiva, divergência, cancelamento de saldo, relação com estoque e eventual tratamento de serviços.
