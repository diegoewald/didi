# FinançasPro V1.6.1

FinançasPro é um app de finanças pessoais feito com **React + TypeScript + Vite**. Ele continua **local-first** com IndexedDB, importação Excel/CSV e backup local, e na V1.6.1 adiciona **login e sincronização opcional com Supabase** para usar os mesmos dados no PC e no celular.

## Modos de uso

### Modo local
- Funciona sem conta e sem Supabase.
- Dados ficam apenas no IndexedDB do navegador/dispositivo.
- Backup JSON/criptografado continua sendo recomendado.
- PC e celular **não sincronizam automaticamente** neste modo.

### Modo online
- Exige `VITE_SUPABASE_URL` e `VITE_SUPABASE_ANON_KEY`.
- Usa Supabase Auth para cadastro/login por email e senha.
- Sincroniza lançamentos, categorias, contas, cartões, orçamentos, metas e configurações.
- Mantém cache local no IndexedDB.
- Usa Row Level Security: cada usuário acessa apenas os próprios dados.

> Segurança: a chave `anon` pública pode ir para o frontend. **Nunca coloque service role key no frontend**.

## Instalação

```bash
npm install
```

## Rodar no PC

```bash
npm run dev
```

Abra `http://localhost:5173/`.

## Rodar no celular pelo host do PC

```bash
npm run dev:host
# ou
npm run dev -- --host 0.0.0.0
```

1. PC e celular precisam estar no mesmo Wi-Fi.
2. Descubra o IPv4 do PC (`ipconfig` no Windows, `ifconfig`/`ip addr` no Linux/macOS).
3. Abra no celular: `http://IP-DO-PC:5173/`.
4. Mantenha o terminal aberto e libere o firewall se necessário.

## Build, preview, testes e lint

```bash
npm run build
npm run preview
npm run test
npm run lint
```

## Configurar Supabase para sincronização

1. Crie um projeto em <https://supabase.com>.
2. Ative Auth por email/senha.
3. Abra o SQL Editor e execute `supabase/schema.sql`.
4. Copie `.env.example` para `.env.local`:

```bash
cp .env.example .env.local
```

5. Preencha:

```env
VITE_SUPABASE_URL=https://SEU-PROJETO.supabase.co
VITE_SUPABASE_ANON_KEY=SUA_CHAVE_ANON_PUBLICA
```

6. Reinicie o Vite.

O arquivo `supabase/schema.sql` cria as tabelas:
- `transactions`
- `categories`
- `accounts`
- `credit_cards`
- `budgets`
- `goals`
- `settings`
- `sync_queue` (reservada para auditoria/expansão futura)

> Se você aplicou o schema experimental da V1.6.0 em um projeto de teste, reaplique `supabase/schema.sql`. A V1.6.1 usa chave primária composta `(user_id, id)` para evitar colisão entre usuários com IDs locais iguais.

Todas possuem `id`, `user_id`, `data`, `created_at`, `updated_at`, `deleted_at` e RLS para `user_id = auth.uid()`.

## Login e cadastro

- Acesse a tela **Entrar** ou **Configurações → Entrar para sincronizar**.
- Crie uma conta ou entre com email/senha.
- Se o Supabase exigir confirmação de email, confirme antes de usar.

## Migrar dados locais para a conta online

1. Faça login.
2. Vá em **Configurações**.
3. Clique em **Exportar backup antes de sincronizar**.
4. Clique em **Migrar dados locais para conta**.
5. Confirme a migração.

A migração:
- não apaga IndexedDB automaticamente;
- mantém IDs quando possível;
- evita duplicação por ID/externalId quando os dados são mesclados;
- mostra resumo de registros enviados;
- preserva dados locais se houver falha.

## Como a sincronização funciona

- Toda alteração salva primeiro no IndexedDB.
- Se o usuário estiver logado, a operação entra em uma fila local e é enviada ao Supabase.
- Se estiver offline ou houver erro, a fila permanece no dispositivo.
- Quando a conexão volta, o app tenta sincronizar novamente.
- Conflitos usam regra simples: **última alteração vence** por `updatedAt`.
- Exclusões online usam `deleted_at` para soft delete.

## Importação Excel/CSV

A tela **Importar** aceita `.xlsx` e `.csv`, valida colunas, mostra prévia e evita duplicidade por `ID externo` ou chave de data/descrição/valor. Após importar, a prévia some, aparece mensagem de sucesso e o botão **Importar outra planilha** fica disponível.

## Backup

- **Backups → Exportar backup** gera JSON local.
- Backup criptografado usa Web Crypto quando disponível.
- Em HTTP por IP local, alguns navegadores bloqueiam `crypto.subtle`; nesse caso use backup simples ou publique em HTTPS.
- Restaurar backup nunca exige Supabase.

## Deploy Vercel

- Build command: `npm run build`
- Output directory: `dist`
- Configure `VITE_SUPABASE_URL` e `VITE_SUPABASE_ANON_KEY` em Project Settings → Environment Variables.
- `vercel.json` mantém fallback de SPA.

## Deploy Netlify

- Build command: `npm run build`
- Publish directory: `dist`
- Configure as variáveis em Site settings → Environment variables.
- `netlify.toml` e `public/_redirects` mantêm fallback de SPA.

## Limitações atuais

- Não existe service role no frontend e não deve existir.
- A sincronização é eventual, não em tempo real por websocket.
- Conflitos usam última alteração vence.
- Não há criptografia ponta a ponta dos dados no banco nesta versão.

## Futuro

- Realtime Supabase opcional.
- Criptografia ponta a ponta.
- Melhor tela de conflitos.
- Backup em nuvem controlado pelo usuário.
