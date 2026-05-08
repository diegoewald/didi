# FinançasPro

**Versão 1.4.2** — ultra revisão técnica com reconstrução do tema claro/escuro, importação mais segura, mensagens mais claras e revisão de estabilidade antes da V1.5 mobile.

FinançasPro é uma aplicação React + TypeScript local-first para organização de finanças pessoais/familiares. Ela oferece dashboard, lançamentos, categorias, contas, cartões, orçamentos, metas, resumo mensal, relatórios, importação Excel/CSV, backup JSON/criptografado e persistência no IndexedDB do navegador.

## Instalação

```bash
npm install
```

> Os dados ficam somente no IndexedDB do navegador usado. Exporte backups antes de limpar dados do navegador, trocar de máquina ou testar restauração.

## Rodar em desenvolvimento

```bash
npm run dev
```

Depois abra a URL exibida pelo Vite, normalmente `http://localhost:5173/`.

### Abrir no celular pela rede local

Para testar rapidamente em outro dispositivo conectado à mesma rede Wi-Fi:

```bash
npm run dev -- --host 0.0.0.0
```

Use no celular o endereço IP do computador com a porta exibida pelo Vite, por exemplo `http://192.168.0.10:5173/`. Esta V1.4.2 não é a V1.5 mobile; o objetivo aqui é apenas facilitar acesso local para conferência.

## Testes e qualidade

```bash
npm run test
npm run lint
npm run build
```

- `npm run test`: executa Vitest para funções críticas de importação, parsing, cálculos e CRUD.
- `npm run lint`: executa ESLint.
- `npm run build`: valida TypeScript e gera build de produção com Vite.

## Tema claro/escuro/sistema

Acesse **Configurações** (`/configuracoes`) e escolha:

- **Sistema**: acompanha `prefers-color-scheme` do navegador/sistema operacional.
- **Claro**: força tema claro.
- **Escuro**: força tema escuro.

A preferência é salva no IndexedDB e também espelhada em cache local para reduzir piscadas de tema durante o carregamento inicial. O botão **Tema** no cabeçalho alterna rapidamente entre claro e escuro.

## Como importar planilha

1. Acesse **Importar** (`/importar`).
2. Selecione um arquivo `.xlsx` ou `.csv`.
3. Revise o mapeamento detectado automaticamente.
4. Clique em **Validar dados e gerar prévia**.
5. Confira erros, duplicados e a prévia.
6. Clique em **Confirmar importação**.

Após a confirmação, a prévia é encerrada para evitar clique duplo ou reimportação acidental. O app mostra a mensagem “Importação concluída com sucesso. X lançamentos foram importados.” quando há novos lançamentos, desabilita a confirmação daquela prévia e exibe **Importar outra planilha**.

A deduplicação usa `ID externo` quando existir; caso contrário, usa data + descrição normalizada + valor. Isso protege contra a mesma planilha ser enviada novamente.

## Backup e restauração

Acesse **Backups** (`/backups`) ou **Configurações** (`/configuracoes`) para:

- exportar backup JSON simples;
- exportar backup criptografado com senha;
- importar backup simples ou criptografado;
- apagar dados locais com confirmação.

O backup criptografado usa Web Crypto com PBKDF2 + AES-GCM. A conversão Base64 é feita em blocos para reduzir risco de erro em arquivos maiores.

## Arquitetura

- `src/app`: composição da aplicação e rotas client-side.
- `src/components`: layout, UI, tabelas, formulários e wizard de importação.
- `src/features`: módulos de negócio (dashboard, lançamentos, importação, categorias, contas, cartões, orçamentos, metas, resumo mensal, relatórios, backup e configurações).
- `src/lib/db`: IndexedDB via `idb`, seed de dados padrão e restauração/limpeza.
- `src/lib/spreadsheet`: leitura `.xlsx`/`.csv`, mapeamento inteligente, validação e geração de modelo.
- `src/lib/calculations`: cálculos financeiros com `decimal.js`.
- `src/lib/export`: exportações Excel/CSV, backup JSON e backup criptografado.
- `src/lib/crud`: helpers de upsert/delete, orçamento único e prevenção de duplicidade.
- `src/tests`: testes de cálculos, importação, parsing e helpers CRUD.

## Privacidade

O FinançasPro roda localmente no navegador e não exige login, backend ou sincronização online. Nenhum dado financeiro é enviado para servidores pelo app. Faça backup regularmente para evitar perda de dados locais.
