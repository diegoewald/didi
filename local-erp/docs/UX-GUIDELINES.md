# Diretrizes de UX

O ERP é ferramenta de produtividade para uso prolongado em desktop.

## Prioridades

1. teclado e ordem de tab coerente;
2. baixa quantidade de cliques;
3. tabelas densas e legíveis;
4. busca e filtros acessíveis;
5. foco visível;
6. atalhos previsíveis;
7. feedback imediato e mensagens claras;
8. consistência entre módulos.

## Evitar

- layout de landing page;
- cards gigantes sem necessidade;
- espaços vazios excessivos;
- gradientes decorativos;
- animações sem função;
- dezenas de cores;
- modais para operações simples;
- ícones sem rótulo em ações críticas.

## Tabelas

Quando surgirem grandes volumes:

- virtualização;
- paginação ou carregamento incremental;
- ordenação e filtro no banco quando necessário;
- persistência opcional de largura/ordem de colunas;
- ações em lote explícitas;
- exportação desacoplada da tela.


## Navegação por teclado do shell

- o foco inicial da `MainWindow` vai para a primeira ação da navegação lateral;
- `Ctrl+1` a `Ctrl+8` abrem, respectivamente, Visão geral, Cadastros, Compras, Estoque, Vendas, Financeiro, Relatórios e Administração;
- as mesmas teclas do teclado numérico são aceitas quando combinadas com `Ctrl`;
- setas percorrem ciclicamente os itens da navegação lateral;
- `Ctrl+Tab` e `Ctrl+Shift+Tab` percorrem ciclicamente as abas do módulo ativo em Cadastros e Compras;
- `F6` alterna ciclicamente o foco entre navegação lateral, conteúdo e ações da conta;
- os atalhos `Ctrl+1…8` são expostos por tooltip e `AutomationProperties.HelpText` nos itens da navegação;
- o shell não captura `Esc` globalmente: cancelamento e fechamento continuam pertencendo à tela ou diálogo ativo;
- foco visível, ordem real de tabulação, DPI, teclado físico e ergonomia ainda devem ser conferidos no smoke visual/interativo em Windows real.
