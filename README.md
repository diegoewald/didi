# FinançasPro

Aplicação React + TypeScript local-first para organização de finanças pessoais/familiares, com importação Excel/CSV, validação robusta, dashboard, relatórios, backup JSON e persistência em IndexedDB.

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
- `src/features`: módulos de negócio (dashboard, lançamentos, categorias, contas, cartões, orçamentos, metas, relatórios e backup).
- `src/lib/db`: IndexedDB via `idb`, com seed de dados padrão.
- `src/lib/spreadsheet`: leitura de `.xlsx`/`.csv`, mapeamento inteligente, validação e geração do modelo.
- `src/lib/calculations`: cálculos financeiros com `decimal.js`.
- `src/tests`: testes de cálculos e importação com Vitest.

## Como importar planilha

1. Acesse `/importar`.
2. Clique em **Baixar modelo de planilha** para gerar `modelo-financaspro.xlsx`.
3. Preencha as colunas padrão ou use nomes similares como `vlr`, `histórico`, `grupo`, `lançamento`.
4. Envie o `.xlsx` ou `.csv`.
5. Revise o mapeamento, valide os dados, veja a prévia e confirme.

## Backup

Acesse `/backups` para exportar `backup-financaspro-DD-MM-AAAA.json`, restaurar um backup ou apagar dados com confirmação forte.

## Privacidade

O FinançasPro roda localmente no navegador e não exige APIs externas. Dados financeiros ficam no IndexedDB do próprio usuário.
