# FinançasPro

**Versão 1.4** — CRUD completo nas telas internas de configurações, contas, cartões, metas, orçamentos e categorias, com fluxo de importação protegido contra reenvio acidental.

Aplicação React + TypeScript local-first para organização de finanças pessoais/familiares, com importação Excel/CSV, validação robusta, dashboard, relatórios, backup JSON/criptografado e persistência em IndexedDB.

## Rodar localmente

```bash
npm install
npm run dev
npm run build
npm run test
npm run lint
```

## Arquitetura

- `src/app`: composição da aplicação e rotas client-side.
- `src/components`: layout, UI, tabelas, formulários e wizard de importação.
- `src/features`: módulos de negócio (dashboard, lançamentos, resumo mensal, categorias, contas, cartões, orçamentos, metas, relatórios, backup e configurações).
- `src/lib/db`: IndexedDB via `idb`, com seed de dados padrão.
- `src/lib/spreadsheet`: leitura de `.xlsx`/`.csv`, mapeamento inteligente, validação, suporte a extratos comuns e geração do modelo.
- `src/lib/calculations`: cálculos financeiros com `decimal.js`.
- `src/lib/export`: exportações Excel/CSV, backup JSON e backup criptografado com Web Crypto.
- `src/lib/crud`: helpers isolados para upsert/delete, orçamento único e prevenção de duplicidade de importação.
- `src/tests`: testes de cálculos, importação e helpers CRUD.

## Configurações

Acesse `/configuracoes` para escolher tema claro/escuro/sistema, primeiro dia do mês financeiro, visão caixa/competência, exportar/importar backup e apagar dados com confirmação forte. As configurações são salvas no IndexedDB e persistem após recarregar.

## Contas

Acesse `/contas` para adicionar, editar, ativar/desativar e excluir contas. Se houver lançamentos vinculados, o app solicita uma conta de destino antes de excluir para evitar perda de vínculo.

## Cartões

Acesse `/cartoes` para cadastrar cartões, editar limite, melhor dia de compra, fechamento, vencimento, conta associada e cor. A ação de pagar fatura fica identificada como “em breve”, sem botão falso.

## Metas

Acesse `/metas` para criar, editar e excluir metas financeiras com nome, valor alvo, valor atual, data alvo, categoria e cor. A tela mostra progresso percentual e simulação mensal.

## Orçamentos

Acesse `/orcamentos` para criar, editar e excluir limites mensais por categoria. O app mostra gasto realizado, percentual consumido e alerta visual acima de 80% ou 100%. Não é permitido duplicar orçamento da mesma categoria no mesmo mês.

## Como importar planilha sem duplicar dados

1. Acesse `/importar`.
2. Selecione um `.xlsx` ou `.csv`.
3. Revise o mapeamento, valide e confira a prévia.
4. Clique em **Confirmar importação** uma única vez.
5. Após sucesso, o botão é desabilitado e a mensagem informa quantos lançamentos foram importados.
6. Para novo arquivo, clique em **Importar outra planilha**.

Além disso, o store filtra transações já existentes por `ID externo` ou por combinação de data, descrição e valor, evitando duplicidade mesmo se a mesma prévia for enviada novamente.

## Backup

Use `/backups` ou `/configuracoes` para exportar backup JSON, importar backup ou limpar todos os dados. Backups criptografados continuam disponíveis na tela de backups.

## Privacidade

O FinançasPro roda localmente no navegador e não exige APIs externas. Dados financeiros ficam no IndexedDB do próprio usuário; exporte backups regularmente para evitar perda ao limpar dados do navegador ou trocar de dispositivo.
