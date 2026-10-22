# Decisão de Adjudicação de RFQ — especificação do quarto slice de Compras

Status: **especificado; implementação do núcleo pendente**.

Este documento define o quarto slice vertical de Compras. O objetivo é registrar, de forma manual e auditável, qual resposta finalizada foi escolhida para cada linha de um RFQ encerrado, sem transformar essa decisão em Pedido de Compra e sem introduzir aprovação, ranking ou recomendação automática.

As decisões abaixo são decisões de produto mínimas e reversíveis para este ERP local-first. Não representam processo interno de empregador, fornecedor ou organização real.

## 1. Motivação e referências

A sequência já implementada é `Solicitação de Compra -> RFQ -> respostas -> comparação`. O `PROJECT-BRIEF.md` prevê comparação de fornecedores antes de Pedido de Compra.

Referências oficiais mostram que adjudicação é uma etapa própria e pode ter granularidade diferente conforme o produto:

- Microsoft Dynamics 365 separa comparação das ações de aceitar/rejeitar propostas e depois apresenta o Pedido de Compra gerado;
- SAP trata comparação e award como etapas distintas e permite adjudicar uma ou várias cotações;
- Oracle permite adjudicar uma negociação inteira, uma linha ou dividir uma linha, e também permite concluir award sem criar um Pedido de Compra.

Referências:

- https://learn.microsoft.com/en-us/dynamics365/supply-chain/procurement/tasks/enter-compare-rfq-bids-award-contracts
- https://help.sap.com/docs/SAP_S4HANA_CLOUD/0e602d466b99490187fcbb30d1dc897c/f16d85d2491247a98c403654210adfd3.html
- https://docs.oracle.com/en/cloud/saas/procurement/26b/oaprc/how-you-award-negotiations.html

Essas referências sustentam a separação `comparar -> decidir/adjudicar -> eventualmente gerar documento de compra`. Elas não tornam obrigatório menor preço, scoring, split award, aprovação ou geração automática de PO.

## 2. Limite do slice

O quarto slice deve permitir:

- criar uma decisão de adjudicação para um RFQ em status `Closed`;
- existir no máximo uma decisão de adjudicação por RFQ;
- manter a decisão em `Draft` enquanto escolhas ainda podem ser alteradas;
- selecionar manualmente, para cada linha do RFQ, uma linha de resposta pertencente a uma `SupplierQuotation` `Finalized`;
- remover ou substituir uma seleção enquanto a decisão estiver em `Draft`;
- deixar linhas do RFQ sem adjudicação;
- registrar observação geral opcional da decisão;
- finalizar a decisão quando existir ao menos uma linha adjudicada;
- registrar usuário e horário de criação e finalização;
- consultar a decisão finalizada e suas seleções posteriormente.

Este slice **não cria Pedido de Compra**.

## 3. Granularidade escolhida

A persistência será por **linha de RFQ**.

Cada linha pode ter:

- nenhuma seleção; ou
- exatamente uma linha de resposta finalizada selecionada.

A primeira versão adjudica a **linha inteira** ao fornecedor selecionado. Não existe quantidade adjudicada parcial neste slice.

Razões:

1. o modelo atual de `SupplierQuotationLine` não registra quantidade ofertada/capacidade do fornecedor;
2. permitir split sem quantidade ofertada criaria uma regra comercial que os dados atuais não sustentam;
3. seleção por linha é mais flexível do que obrigar um único fornecedor para o RFQ inteiro;
4. adjudicar todas as linhas ao mesmo fornecedor continua possível selecionando a mesma resposta em cada linha;
5. split/quantidade parcial poderá ser adicionado depois, se houver requisito explícito, sem invalidar a decisão por linha já existente.

## 4. Estados

`RequestForQuotationAwardStatus` terá:

- `Draft` — escolhas e observação podem ser alteradas;
- `Finalized` — decisão imutável neste slice.

Não haverá `Cancelled` nesta primeira versão. Um `Draft` pode ser corrigido em vez de apagado; uma decisão `Finalized` não pode ser reaberta ou substituída sem uma futura regra explícita de revisão/anulação.

## 5. Pré-condição do RFQ

A decisão só pode ser criada para `RequestForQuotationStatus.Closed`.

Não criar decisão para RFQ `Draft`, `Sent` ou `Cancelled`.

O fechamento do RFQ já exige ao menos uma resposta finalizada. A adjudicação acontece depois do encerramento do processo de coleta de propostas.

## 6. Seleção válida

Para selecionar uma oferta para uma linha:

- a decisão deve estar `Draft`;
- a linha de RFQ deve pertencer ao mesmo RFQ da decisão;
- a `SupplierQuotationLine` deve pertencer a uma `SupplierQuotation` do mesmo RFQ;
- essa `SupplierQuotation` deve estar `Finalized`;
- a linha de resposta deve referenciar exatamente a linha de RFQ que está sendo adjudicada;
- o fornecedor da resposta deve continuar sendo um fornecedor convidado daquele RFQ, como garantido pelas invariantes do agregado de RFQ.

Uma resposta `Draft` nunca pode ser adjudicada.

## 7. Finalização

`Finalize` exige:

- status atual `Draft`;
- pelo menos uma linha selecionada;
- usuário autenticado e ativo;
- timestamp UTC.

Não é obrigatório adjudicar todas as linhas do RFQ. Linhas sem seleção permanecem explicitamente não adjudicadas.

Após `Finalized`:

- observação não muda;
- seleções não mudam;
- nenhuma linha é adicionada/removida/substituída;
- o registro permanece somente leitura.

## 8. Observação

A decisão possui `Notes` opcional, normalizada e limitada a 1000 caracteres.

Não exigir justificativa obrigatória neste slice, pois isso seria regra organizacional não confirmada. A observação existe para contexto quando necessário.

## 9. Autorização

Leitura da decisão reutiliza `request-for-quotations.read`.

Mutações usam uma permissão nova e específica:

- `request-for-quotations.award`

Motivo: adjudicar tem impacto maior do que editar um RFQ e não deve ser concedido implicitamente a toda role com `request-for-quotations.manage`.

A nova permissão entra no `PermissionCatalog` e no bootstrap de instalações novas. Roles existentes não recebem grant automático.

## 10. Modelo de domínio previsto

### `RequestForQuotationAward`

- `Id`;
- `RequestForQuotationId`;
- `Status`;
- `CreatedByUserId`;
- `CreatedAtUtc`;
- `Notes`;
- `FinalizedByUserId?`;
- `FinalizedAtUtc?`;
- coleção de `RequestForQuotationAwardLine`.

### `RequestForQuotationAwardLine`

- `Id`;
- `RequestForQuotationAwardId`;
- `RequestForQuotationLineId`;
- `SupplierQuotationLineId`.

Não duplicar preço, moeda, fornecedor ou descrição nesta tabela enquanto as referências finalizadas e imutáveis existentes forem suficientes para reconstruir a decisão.

## 11. Application prevista

Casos de uso mínimos:

- `CreateDraftAsync(requestForQuotationId, notes?)`;
- `GetByIdAsync(id)`;
- `GetByRequestForQuotationIdAsync(requestForQuotationId)`;
- `UpdateDraftAsync(id, notes?)`;
- `SelectLineAsync(id, requestForQuotationLineId, supplierQuotationLineId)`;
- `RemoveLineSelectionAsync(id, requestForQuotationLineId)`;
- `FinalizeAsync(id)`.

A Application valida referências cruzadas carregando o RFQ antes de aplicar uma seleção.

## 12. Persistência prevista

Adicionar:

- `RequestForQuotationAwards`;
- `RequestForQuotationAwardLines`.

Integridade mínima:

- FK `RequestForQuotationAwards.RequestForQuotationId -> RequestForQuotations.Id` com `RESTRICT`;
- FK de usuários de criação/finalização com `RESTRICT`;
- unicidade de `RequestForQuotationId` para garantir uma decisão por RFQ;
- FK de linha filha para o award com `CASCADE` apenas dentro do agregado;
- FK para `RequestForQuotationLines` com `RESTRICT`;
- FK para `SupplierQuotationLines` com `RESTRICT`;
- unicidade `(RequestForQuotationAwardId, RequestForQuotationLineId)`.

A migration deve ser gerada pelo EF Core, validada por snapshot e pelo ciclo apply -> rollback 0 -> reapply.

## 13. UI prevista

Depois de Domain/Application/Persistence estarem verdes:

- RFQ `Closed` deve oferecer ação `Adjudicar`/`Ver decisão`;
- a tela mostra as linhas do RFQ e somente ofertas finalizadas válidas para cada linha;
- nenhuma proposta é pré-selecionada por preço;
- preço, moeda, fornecedor e data prometida ficam visíveis para apoiar a escolha manual;
- linhas podem permanecer sem seleção;
- decisão em `Finalized` fica totalmente somente leitura;
- não haverá botão `Gerar Pedido` neste slice.

## 14. Fora do escopo

Não implementar neste slice:

- split de uma mesma linha entre vários fornecedores;
- quantidade adjudicada parcial;
- winner/ranking automático;
- recomendação por menor preço;
- scoring/pesos;
- conversão cambial;
- impostos, frete, desconto ou condição de pagamento;
- aprovação/alçada;
- rejeição formal dos fornecedores não selecionados;
- revisão/anulação de decisão finalizada;
- Pedido de Compra;
- recebimento, estoque, financeiro ou fiscal.

## 15. Critério de conclusão

O slice estará concluído quando:

1. Domain impedir alteração fora de `Draft` e exigir ao menos uma seleção na finalização;
2. Application exigir RFQ `Closed`, validar cada seleção contra uma resposta `Finalized` do mesmo RFQ/linha e aplicar autorização correta;
3. uma decisão por RFQ for garantida também pelo banco;
4. persistência e migration estiverem cobertas por testes;
5. WPF permitir seleção manual por linha e consulta somente leitura após finalização;
6. build/testes/snapshot/migrations permanecerem verdes no CI.

## 16. Etapa seguinte

Somente depois da decisão de adjudicação estar operacional deverá ser especificado o **Pedido de Compra**. Essa futura especificação deverá decidir campos obrigatórios do pedido, agrupamento de linhas adjudicadas, numeração, status, alteração/cancelamento, vínculo com fornecedor, preço/moeda, datas, aprovação e relação com recebimento.
