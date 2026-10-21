# Compras — descoberta e definição do primeiro fluxo

Status: **descoberta funcional em andamento**.

Este documento define o limite seguro para iniciar a Fase 7 sem transformar padrões comuns de mercado em regras de negócio inventadas.

## 1. Objetivo

O módulo de Compras deverá evoluir, ao longo do projeto, para suportar os conceitos previstos no `PROJECT-BRIEF.md`:

- solicitação de compra;
- cotação;
- comparação de fornecedores;
- pedido de compra;
- acompanhamento;
- recebimento;
- histórico;
- anexos;
- aprovação.

Isso é planejamento de produto, não significa que todas essas etapas sejam obrigatórias em todo processo nem que devam ser implementadas no primeiro slice.

## 2. Referências externas consultadas

A pesquisa de referência foi usada apenas para identificar padrões de mercado e pontos que normalmente são configuráveis. Ela **não substitui requisito de negócio do ERP**.

### Microsoft Dynamics 365

A documentação de Purchase Requisitions trata a requisição como documento interno e mostra um workflow configurável que pode partir de `Draft`, passar por revisão/aprovação e, após aprovação, permitir geração de pedido de compra. O fluxo pode revisar o documento inteiro ou linhas individualmente, e a própria documentação orienta a organização a definir quem aprova, o que precisa ser revisado e quando uma aprovação pode ser automática.

Referências:

- https://learn.microsoft.com/en-us/dynamics365/supply-chain/procurement/purchase-requisitions-overview
- https://learn.microsoft.com/en-us/dynamics365/supply-chain/procurement/purchase-requisitions-workflow

### Oracle Procurement

A documentação de ciclo de vida da requisição representa o fluxo como uma cadeia que pode conter linhas da requisição, negociação, pedidos, remessas, recebimentos e documentos posteriores. Uma requisição pode inclusive alimentar mais de um pedido, dependendo da configuração e do processamento das linhas.

Referências:

- https://docs.oracle.com/en/cloud/saas/procurement/25c/oaprc/requisition-life-cycle.html
- https://docs.oracle.com/en/cloud/saas/procurement/26d/oaprc/purchase-order-life-cycle.html

### SAP

A documentação de processo de pedido de compra descreve um fluxo em que uma requisição é submetida, aprovada ou negada e, quando aprovada, pode originar um pedido enviado ao fornecedor. Esse comportamento pertence ao produto/configuração da SAP e não será copiado automaticamente para este ERP.

Referência:

- https://help.sap.com/docs/buying-invoicing/purchasing-guide-for-procurement-professionals/purchase-order-process-6b32ab8fc1da1014bbcef40b7787d981

## 3. Conclusão da pesquisa

Há um padrão conceitual recorrente:

`necessidade interna -> requisição -> processo de compra -> pedido -> acompanhamento -> recebimento`

Porém, os pontos mais importantes variam por organização:

- se requisição é obrigatória;
- se cotação é obrigatória;
- quantidade mínima de fornecedores em uma cotação;
- quando existe aprovação;
- quem aprova;
- limites de valor/alçada;
- se linhas podem seguir caminhos diferentes;
- se uma requisição gera um ou vários pedidos;
- se várias requisições podem ser consolidadas;
- como funcionam alterações, cancelamentos e reaberturas;
- como funciona recebimento parcial;
- quais vínculos organizacionais são obrigatórios.

Portanto, nenhuma dessas regras será hardcoded apenas porque aparece em SAP, Oracle, Dynamics ou outro ERP.

## 4. Restrições já confirmadas pelo projeto

Independentemente do fluxo funcional escolhido, Compras deve respeitar as regras arquiteturais já consolidadas:

- funcionamento local-first;
- regras e autorização na camada Application, nunca somente na UI;
- persistência por migrations reproduzíveis;
- foreign keys e constraints reais quando houver relação obrigatória;
- alterações importantes preparadas para auditoria;
- logs sem exposição de dados sensíveis;
- UI WPF focada em produtividade desktop;
- nenhuma integração automática com Estoque, Financeiro, Fiscal ou Contábil sem requisito explícito;
- nenhuma elevação implícita de permissões para roles existentes;
- nenhuma regra comercial presumida a partir dos cadastros-base.

## 5. Cadastros hoje disponíveis

A Fase 6 disponibiliza:

- Empresa;
- Filial;
- Fornecedor;
- Material;
- Produto;
- Unidade de Medida;
- Categoria;
- Centro de Custo;
- Usuários e roles/permissões.

A existência desses cadastros **não torna seu vínculo obrigatório em Compras**. Por exemplo, não será assumido que toda solicitação precisa de Centro de Custo ou que todo item comprado precisa ser um Material cadastrado até isso ser definido como requisito.

## 6. Fluxo conceitual alvo

O módulo deve ser desenhado para conseguir representar futuramente, sem reescrita estrutural, a seguinte cadeia:

1. necessidade/solicitação interna;
2. eventual revisão ou aprovação;
3. eventual cotação com um ou mais fornecedores;
4. eventual comparação e escolha;
5. emissão de pedido de compra;
6. acompanhamento do pedido;
7. recebimento total ou parcial, se essa regra for adotada;
8. histórico do ciclo;
9. anexos relacionados aos documentos, quando implementados.

A palavra **eventual** é intencional: obrigatoriedade, atalhos e exceções ainda dependem de regra funcional confirmada.

## 7. Primeiro slice recomendado

O menor slice vertical recomendado é **Solicitação de Compra**, antes de Cotação e Pedido.

A implementação só deve começar quando for possível responder, de forma explícita, às decisões bloqueadoras da próxima seção.

O primeiro slice não deve tentar resolver de uma vez cotação, aprovação, pedido, recebimento, anexos e estoque. Isso reduzirá risco de modelar relações erradas cedo demais.

## 8. Decisões bloqueadoras antes da primeira entidade

As decisões abaixo ainda não estão definidas no repositório e **não devem ser respondidas por suposição**:

1. **O que pode ser solicitado?**
   - somente Material;
   - Material e Produto;
   - Serviço;
   - item livre/não cadastrado;
   - combinação dos anteriores.

2. **Quais vínculos organizacionais são obrigatórios?**
   - Empresa;
   - Filial;
   - Centro de Custo;
   - nenhum deles;
   - combinação deles.

3. **Quais dados mínimos uma linha de solicitação precisa ter?**
   - quantidade;
   - unidade;
   - descrição;
   - data de necessidade;
   - observação;
   - outros campos realmente necessários.

4. **Quais estados reais existem na Solicitação de Compra?**
   - por exemplo, `Draft`, `Submitted`, `Approved`, `Rejected`, `Cancelled` são comuns em outros sistemas, mas não serão adotados sem confirmação.

5. **Quem pode criar, editar, enviar, cancelar e consultar?**
   - qualquer usuário com permissão;
   - somente o solicitante;
   - comprador;
   - aprovador;
   - combinação por estado.

6. **Existe aprovação no primeiro fluxo?**
   - sempre;
   - nunca;
   - por valor;
   - por Centro de Custo;
   - por solicitante/role;
   - outra regra.

7. **Cotação é obrigatória antes do Pedido?**
   - se sim, em quais casos e com quantos fornecedores;
   - se não, quais situações permitem pedido direto.

8. **Como requisição e pedido se relacionam?**
   - uma solicitação gera um pedido;
   - uma solicitação pode gerar vários pedidos;
   - várias solicitações podem ser consolidadas em um pedido;
   - comportamento por linha.

9. **Recebimento parcial deve existir?**
   - e, se existir, como controlar saldo pendente, encerramento e cancelamento de saldo.

10. **Como deve funcionar a numeração dos documentos?**
    - GUID apenas interno;
    - número sequencial amigável;
    - sequência por empresa/filial/ano;
    - outra regra.

11. **Quais alterações são permitidas após envio/aprovação/emissão?**
    - edição direta;
    - revisão/versionamento;
    - cancelamento e nova emissão;
    - nenhuma alteração.

12. **Quais eventos precisam de histórico/auditoria funcional visível?**
    - além da trilha técnica já prevista na arquitetura.

## 9. Regras que NÃO serão presumidas

Até definição explícita, não implementar automaticamente:

- três cotações obrigatórias;
- fornecedor vencedor por menor preço;
- aprovação automática ou por valor;
- alçadas hierárquicas;
- vínculo obrigatório com Centro de Custo;
- vínculo obrigatório com Empresa/Filial;
- moeda, impostos, frete ou condição de pagamento;
- lead time contratual;
- estoque reservado ou entrada automática;
- contas a pagar;
- regras fiscais;
- integração com nota fiscal;
- pedido internacional/importação;
- fornecedor padrão;
- preços históricos como regra de decisão;
- consolidação automática de solicitações;
- aprovação linha a linha.

## 10. Privacidade e dados de desenvolvimento

A modelagem e os testes do módulo devem usar dados fictícios. Processos internos, fornecedores, preços, pedidos, contatos ou outros dados sensíveis de empregadores/empresas reais não devem ser usados como seed, fixture, documentação ou exemplo do repositório.

## 11. Critério para sair da descoberta

A descoberta de Compras estará concluída quando as decisões necessárias ao primeiro slice estiverem documentadas de forma suficiente para definir:

- entidade(s);
- estados;
- atores;
- permissões;
- invariantes;
- transições;
- persistência;
- testes de domínio e aplicação;
- primeira tela WPF.

Somente então o roadmap deve marcar o primeiro slice de Compras como pronto para implementação.

## 12. Próxima prioridade

Resolver as decisões bloqueadoras especificamente para **Solicitação de Compra** e transformar as respostas em uma especificação executável de estados, permissões e invariantes.

Até isso ocorrer, criar entidades como `PurchaseRequest`, `Quotation`, `PurchaseOrder` ou `Receipt` seria prematuro e contrariaria o princípio do projeto de não inventar regra de negócio.
