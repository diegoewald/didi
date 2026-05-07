# FinançasPro

Aplicação React + TypeScript local-first para organização de finanças pessoais/familiares, com importação Excel/CSV, validação robusta, dashboard, relatórios, backup JSON/criptografado e persistência em IndexedDB.

## Rodar localmente

```bash
npm install
npm run dev
npm run build
npm run test
```

## Arquitetura

- `src/app`: composição da aplicação e rotas client-side.
- `src/components`: layout, UI, tabelas, formulários e wizard de importação.
- `src/features`: módulos de negócio (dashboard, lançamentos, resumo mensal, categorias, contas, cartões, orçamentos, metas, relatórios e backup).
- `src/lib/db`: IndexedDB via `idb`, com seed de dados padrão.
- `src/lib/spreadsheet`: leitura de `.xlsx`/`.csv`, mapeamento inteligente, validação, suporte a extratos comuns e geração do modelo.
- `src/lib/calculations`: cálculos financeiros com `decimal.js`.
- `src/lib/export`: exportações Excel/CSV, backup JSON e backup criptografado com Web Crypto.
- `src/tests`: testes de cálculos e importação com Vitest.

## Como importar planilha ou extrato

1. Acesse `/importar`.
2. Clique em **Baixar modelo de planilha** para gerar `modelo-financaspro.xlsx`.
3. Preencha as colunas padrão ou use nomes similares como `vlr`, `histórico`, `grupo`, `lançamento`.
4. Para extratos bancários comuns, o importador também reconhece `Data`, `Histórico`, `Valor`, `Saldo`, `Crédito` e `Débito`; valores negativos são classificados como despesas.
5. Envie o `.xlsx` ou `.csv`.
6. Revise o mapeamento, valide os dados, veja a prévia e confirme.

## Lançamentos e filtros

A tela `/lancamentos` permite criar, editar, excluir, duplicar e marcar lançamentos como pagos. Os filtros cobrem mês, ano, conta, categoria, tipo, status e busca textual.

## Resumo mensal e relatórios

Acesse `/resumo-mensal` para ver entradas, saídas, saldo, maiores gastos e categorias do mês. Use **Exportar Excel mensal** para baixar um relatório com abas de resumo, lançamentos e categorias. A tela `/relatorios` também oferece exportação Excel, CSV e relatório mensal.

## Backup

Acesse `/backups` para exportar `backup-financaspro-DD-MM-AAAA.json`, gerar backup criptografado com senha, restaurar backups simples/criptografados ou apagar dados com confirmação forte.

## Privacidade

O FinançasPro roda localmente no navegador e não exige APIs externas. Dados financeiros ficam no IndexedDB do próprio usuário; exporte backups regularmente para evitar perda ao limpar dados do navegador ou trocar de dispositivo.
