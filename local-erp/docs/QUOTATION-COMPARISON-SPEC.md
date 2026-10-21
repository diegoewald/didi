# Comparação de cotações — especificação do terceiro slice de Compras

Status: **especificado; implementação pendente**.

Este documento define o terceiro slice vertical de Compras. Ele usa os RFQs e respostas já registrados para oferecer uma comparação operacional somente leitura, sem transformar comparação em adjudicação, aprovação ou geração de Pedido de Compra.

## 1. Motivação e referências

O `PROJECT-BRIEF.md` prevê `cotação -> comparação de fornecedores -> pedido de compra`. O segundo slice de Cotação/RFQ já está implementado e termina sem escolher vencedor.

Referências oficiais tratam a comparação como uma etapa própria: Dynamics 365 expõe uma tela de comparação de respostas antes das ações de aceitar/rejeitar; SAP possui uma aplicação específica para comparar cotações e deixa a adjudicação como ação posterior; Oracle separa respostas, avaliação e award dentro do processo de sourcing.

Essas referências sustentam apenas a separação conceitual entre **comparar** e **decidir/adjudicar**. Elas não autorizam o ERP a presumir menor preço obrigatório, ranking, pesos, scoring ou vencedor.

Referências:

- https://learn.microsoft.com/en-us/dynamics365/supply-chain/procurement/tasks/enter-compare-rfq-bids-award-contracts
- https://help.sap.com/docs/SAP_S4HANA_ON-PREMISE/af9ef57f504840d2b81be8667206d485/0fd1842b7cb14be4bc38e43d457994dd.html
- https://docs.oracle.com/en/cloud/saas/procurement/26a/fainp/src-terminology.html

## 2. Limite do slice

O terceiro slice deve permitir:

- abrir uma comparação para um RFQ existente;
- visualizar todas as linhas do RFQ com os dados históricos da Solicitação de Compra de origem;
- visualizar todos os fornecedores convidados, inclusive os que não possuem resposta finalizada;
- mostrar o estado da resposta de cada fornecedor (`sem resposta`, `Draft` ou `Finalized`);
- para respostas `Finalized`, mostrar por linha o preço unitário cotado, data prometida e observação da linha;
- mostrar moeda, referência do fornecedor, validade e data de finalização da resposta;
- preservar leitura histórica de fornecedor e Unidade de Medida mesmo se o cadastro for desativado depois;
- funcionar para RFQs `Sent`, `Closed` ou `Cancelled` quando houver histórico; RFQ `Draft` pode ser aberto, mas naturalmente ainda não terá respostas comparáveis.

Este slice é **somente leitura**.

## 3. O que não será feito

Não implementar neste slice:

- vencedor do RFQ;
- aceite/rejeição de fornecedor ou linha;
- ranking obrigatório;
- destaque automático de “melhor” proposta;
- scoring ou pesos;
- recomendação automática;
- quantidade mínima de fornecedores;
- normalização ou conversão cambial;
- comparação de moedas diferentes como se fossem equivalentes;
- cálculo de impostos, frete, descontos ou condição de pagamento;
- cálculo de custo total como regra comercial;
- aprovação;
- Pedido de Compra;
- alteração de RFQ ou resposta a partir da tela de comparação;
- nova tabela ou migration apenas para armazenar a comparação.

## 4. Fonte de verdade

A comparação é uma projeção derivada de dados existentes:

- `RequestForQuotation` e suas linhas;
- `PurchaseRequest` e suas linhas de origem;
- `MeasurementUnit` para nome histórico da unidade;
- `Supplier` para nome/status histórico do fornecedor;
- `SupplierQuotation` e `SupplierQuotationLine`.

Nenhum snapshot duplicado será criado neste slice. As FKs existentes e a política atual de não fazer hard delete público preservam as referências necessárias.

## 5. Regra de respostas

Todos os fornecedores convidados aparecem na comparação.

Para cada fornecedor:

- sem `SupplierQuotation`: estado `Sem resposta`;
- resposta `Draft`: mostrar que há rascunho, mas **não usar seus preços como oferta comparável**;
- resposta `Finalized`: seus dados de cabeçalho e linhas podem entrar na comparação.

A decisão de ignorar preço de resposta `Draft` evita comparar uma proposta ainda mutável com propostas finalizadas.

## 6. Regra por linha

Cada linha da comparação representa uma `RequestForQuotationLine` e resolve sua `PurchaseRequestLine` de origem.

Dados exibidos da origem:

- número da linha no RFQ;
- descrição;
- quantidade solicitada;
- Unidade de Medida;
- data de necessidade, quando existir.

Para cada fornecedor convidado, a célula/entrada da linha contém:

- `UnitPrice` ou ausência de cotação;
- `PromisedDeliveryDate`, quando informada;
- observação da linha, quando informada.

Preço `0` é um preço cotado válido e deve ser distinguido de “linha não cotada”; por isso a projeção usa `decimal?` para preço comparado.

## 7. Moedas

A moeda pertence ao cabeçalho de cada `SupplierQuotation`.

A tela deve deixar a moeda visível junto ao fornecedor/resposta. Se fornecedores responderem em moedas diferentes, os valores são mostrados como registrados, sem conversão, ordenação financeira global ou inferência de equivalência.

## 8. Ordenação determinística

Para produtividade e testes reproduzíveis:

- linhas: `LineNumber` crescente;
- fornecedores: nome ordinal-ignore-case e depois `SupplierId`;
- nenhuma ordenação automática por preço.

## 9. Autorização

A comparação reutiliza `request-for-quotations.read`.

Não será criada permissão nova: quem pode consultar um RFQ pode consultar a projeção de comparação dos mesmos dados.

A autorização continua na Application por `DemandAsync`. Controle visual no WPF não substitui autorização.

## 10. Contrato de Application

Adicionar um serviço/projeção de leitura equivalente a:

`GetComparisonAsync(requestForQuotationId)`

O resultado deverá conter:

- identificação e status do RFQ;
- identificação da Solicitação de origem;
- fornecedores convidados com nome/status cadastral e estado da resposta;
- metadados de resposta finalizada;
- linhas da origem;
- ofertas finalizadas por fornecedor/linha, com preço unitário opcional.

Falhas de integridade referencial inesperadas devem gerar erro explícito, não linha/célula silenciosamente incorreta.

## 11. Persistência

Nenhuma nova entidade, tabela, coluna, migration ou valor monetário persistido será criado por este slice.

A projeção deve usar os repositórios existentes. Portanto, o `has-pending-model-changes` deve continuar limpo.

## 12. UI prevista

Depois do read model da Application estar verde, o WPF deve oferecer ação `Comparar` no RFQ e uma visualização desktop com:

- cabeçalho do RFQ;
- colunas/cartões de fornecedores convidados;
- estado da resposta e moeda visíveis;
- matriz por linha com preço e data prometida;
- indicação clara de `Sem resposta finalizada` e `Linha não cotada`;
- rolagem horizontal quando houver muitos fornecedores;
- nenhum botão de adjudicar/aceitar/gerar pedido neste slice.

## 13. Critério de conclusão

O slice estará concluído quando:

1. a especificação estiver versionada;
2. a Application produzir comparação determinística e autorizada;
3. respostas `Draft` não fornecerem preços comparáveis;
4. fornecedores sem resposta continuarem visíveis;
5. moedas diferentes forem preservadas sem conversão/ranking;
6. a UI WPF apresentar a comparação em modo somente leitura;
7. build/testes/snapshot/migrations permanecerem verdes no CI.

## 14. Próxima etapa após este slice

Somente após a comparação estar operacional o projeto deve especificar uma etapa de **decisão/adjudicação** ou **Pedido de Compra**. Essa especificação futura deverá decidir explicitamente se a escolha é por RFQ inteiro ou por linha, se múltiplos fornecedores podem ser escolhidos, quais justificativas/históricos são obrigatórios e como a decisão se relaciona ao Pedido de Compra.
