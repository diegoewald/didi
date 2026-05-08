# FinançasPro V1.5.6

FinançasPro é um app de finanças pessoais **local-first** feito com React, TypeScript e Vite. Ele ajuda a controlar lançamentos, categorias, contas, cartões, orçamentos, metas, relatórios, importação Excel/CSV e backups.

> **Privacidade:** o app não tem login, backend ou sincronização em nuvem nesta versão. Seus dados ficam no **IndexedDB do navegador** em cada dispositivo.

## O que mudou na V1.5.6

- Correção crítica para abrir no celular pelo IP do PC sem depender de `crypto.randomUUID`.
- Backup criptografado agora falha de forma amigável quando o navegador bloqueia `crypto.subtle` em HTTP por IP local.
- Visual mais estável no desktop e no celular.
- Tema claro/escuro padronizado com tokens globais de UI.
- Menu mobile com acesso a todas as telas.
- Lançamentos em cards no celular e tabela no desktop.
- Importação mais segura contra clique duplo e duplicados.
- Script `dev:host` para abrir no celular usando o PC como servidor local.
- Arquivos para deploy estático em Vercel e Netlify.
- Manifest simples para instalação como app/PWA leve.

## Comandos principais

```bash
npm install
npm run dev
npm run dev:host
npm run build
npm run preview
npm run test
npm run lint
```

## Como instalar

```bash
npm install
```

O projeto não usa `latest` nas dependências. O `package-lock.json` deve ser mantido para instalações reproduzíveis.

## Como rodar no PC

```bash
npm run dev
```

Abra a URL exibida no terminal, normalmente:

```text
http://localhost:5173/
```

## Como rodar no celular pelo host do PC

Use quando quiser testar o FinançasPro no celular sem publicar online.

1. Conecte **PC e celular no mesmo Wi‑Fi**.
2. No PC, rode:

```bash
npm run dev:host
```

3. Descubra o IPv4 do PC:
   - **Windows:** abra CMD/PowerShell e rode `ipconfig`; procure “Endereço IPv4”.
   - **macOS:** rode `ipconfig getifaddr en0` ou veja em Ajustes de Rede.
   - **Linux:** rode `hostname -I` ou `ip addr`.
4. No celular, abra:

```text
http://IP-DO-PC:5173/
```

Exemplo:

```text
http://192.168.0.10:5173/
```

Mantenha o terminal aberto. Se não abrir, confira firewall, antivírus, Wi‑Fi convidado e se o IP está correto.

## Como publicar na Vercel

Configuração esperada:

- **Build command:** `npm run build`
- **Output directory:** `dist`

O arquivo `vercel.json` redireciona rotas internas para `index.html`, então refresh em `/lancamentos`, `/relatorios` etc. não quebra.

## Como publicar na Netlify

Configuração esperada:

- **Build command:** `npm run build`
- **Publish directory:** `dist`

O arquivo `netlify.toml` e `public/_redirects` preparam a aplicação SPA para refresh em rotas internas.

## Preview de produção local

Depois do build:

```bash
npm run build
npm run preview
```

## Tema claro, escuro e sistema

Acesse **Configurações** para escolher:

- **Sistema:** segue o tema do navegador/sistema operacional.
- **Claro:** força tema claro.
- **Escuro:** força tema escuro.

A preferência é salva no IndexedDB e também em cache local para evitar piscada de tema ao abrir o app.

## Como importar Excel/CSV

1. Abra **Importar**.
2. Escolha um arquivo `.xlsx` ou `.csv`.
3. Revise o mapeamento automático.
4. Se necessário, ajuste manualmente as colunas.
5. Clique em **Validar dados e gerar prévia**.
6. Confira erros, duplicados e a prévia.
7. Clique em **Confirmar importação**.

O app aceita:

- Data + Descrição/Histórico + Valor;
- Data + Histórico + Valor negativo;
- Data + Histórico + Crédito + Débito;
- campos extras como Saldo para facilitar mapeamento de extratos.

## Como evitar duplicados

A importação evita duplicidade por:

- `ID externo`, quando a planilha tem esse campo;
- ou combinação de data + descrição normalizada + valor.

Depois de confirmar uma importação, a prévia é escondida, o botão de confirmar fica indisponível e aparece **Importar outra planilha**. Isso evita clique duplo e reimportação acidental.

## Backup e restauração

Abra **Backups** ou **Configurações** para:

- exportar backup JSON simples;
- exportar backup criptografado com senha;
- restaurar backup simples ou criptografado;
- apagar dados locais com confirmação forte.

Backups criptografados usam Web Crypto com PBKDF2 + AES-GCM. A conversão Base64 é feita em blocos para lidar melhor com arquivos grandes. Em alguns navegadores, `http://IP-DO-PC:5173/` não é considerado contexto seguro para `crypto.subtle`; nesse caso o app mostra a mensagem “Backup criptografado exige navegador compatível ou HTTPS. Use backup simples ou publique online em HTTPS.” e continua funcionando com backup simples.

## Avisos importantes sobre dados locais

- Dados ficam no IndexedDB do navegador usado.
- Limpar dados do navegador pode apagar suas informações.
- PC e celular **não sincronizam automaticamente** nesta versão.
- Para mover dados entre dispositivos, exporte backup em um dispositivo e restaure no outro.
- Deploy na Vercel/Netlify não envia seus dados para a nuvem; cada navegador mantém seus próprios dados locais.

## Telas disponíveis

- Dashboard
- Lançamentos
- Importar
- Categorias
- Contas
- Cartões
- Orçamentos
- Metas
- Resumo mensal
- Relatórios
- Backups
- Configurações

No celular, toque em **Mais** na barra inferior para acessar todas as telas.

## Solução de problemas comuns

### O celular não abre o app pelo IP do PC

- Confirme que PC e celular estão no mesmo Wi‑Fi.
- Use `npm run dev:host`, não apenas `npm run dev`.
- Verifique firewall do sistema operacional.
- Confira se a URL usa `http://` e a porta correta.

### Uma rota publicada dá 404 ao atualizar a página

- Na Vercel, confira se `vercel.json` foi enviado.
- Na Netlify, confira `netlify.toml` e `public/_redirects`.

### Importação mostra duplicados

Isso é esperado se o arquivo já foi importado. Revise a lista e importe apenas linhas novas.

### Backup criptografado não restaura

- Confira a senha.
- Se estiver usando o celular em `http://IP-DO-PC:5173/`, alguns navegadores bloqueiam Web Crypto avançado; use backup simples nesse modo ou publique em HTTPS para usar criptografia.
- Use o arquivo JSON criptografado original.
- Se a senha foi perdida, não é possível recuperar esse backup.

### Dados sumiram após limpar navegador

Dados ficam no IndexedDB. Restaure um backup exportado anteriormente.

## Futuro

Não faz parte da V1.5.6, mas pode entrar em versões futuras:

- login;
- sincronização entre dispositivos;
- backup em nuvem;
- criptografia ponta a ponta para dados sincronizados.
