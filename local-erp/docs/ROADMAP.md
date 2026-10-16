# Roadmap

Legenda: `[ ]` pendente, `[-]` em andamento, `[x]` concluído.

## Fase 0 — Pesquisa e arquitetura

- [x] comparar stacks desktop Windows;
- [x] definir .NET 10 LTS + WPF;
- [x] definir SQLite single-PC e PostgreSQL futuro;
- [x] definir arquitetura inicial;
- [x] registrar ADRs iniciais.

## Fase 1 — Fundação técnica

- [x] estruturar solução e projetos;
- [x] configurar `.editorconfig` e análise estática;
- [x] criar shell desktop mínimo;
- [x] separar diretórios de dados do diretório de aplicação;
- [x] configurar logging local;
- [x] executar restore/build/test em Windows com .NET 10 SDK;
- [x] fechar baseline sem warnings/erros;
- [x] manter CI Windows permanente.

## Fase 2 — Persistência

- [x] modelar primeira entidade útil (`Company`);
- [x] criar `DbContext` e migrations iniciais;
- [x] aplicar e validar WAL/foreign keys;
- [x] definir política de valores monetários;
- [x] testar migrations e integridade referencial;
- [x] criar seed de desenvolvimento idempotente.

## Fase 3 — Autenticação e autorização

- [x] abstração e implementação de password hashing;
- [x] usuários locais normalizados e únicos;
- [x] autenticação local;
- [x] sessão local em memória;
- [x] roles e permissões;
- [x] persistência user-role e role-permission;
- [x] autorização deny-by-default na Application;
- [x] bootstrap seguro e atômico do primeiro administrador;
- [x] fluxo de startup bootstrap/login/main;
- [x] telas WPF de primeiro acesso e login;
- [x] logout com retorno ao login;
- [x] testes de regressão do baseline de autenticação;
- [x] lockout/throttling persistente por conta;
- [x] hardening de timing para usuário inexistente/inativo/bloqueado;
- [ ] benchmark do custo de hash em hardware Windows representativo;
- [ ] trilha de auditoria de segurança.

## Fase 4 — Shell da aplicação

- [-] navegação real por módulos;
- [ ] tratamento global de erro além do startup;
- [ ] atalhos e foco/teclado;
- [ ] estados loading/empty/error;
- [ ] componentes de tabela/filtro após existir uso real;
- [ ] smoke visual/interativo do fluxo de autenticação em Windows.

## Fase 5 — Cadastros-base

- [-] empresa/filial: serviço e persistência básicos existem; UI operacional pendente;
- [ ] fornecedores;
- [ ] produtos/materiais;
- [ ] unidades/categorias;
- [ ] centros de custo.

## Fase 6 — Primeiro módulo operacional

- [ ] compras: fluxo mínimo a definir com regras de negócio reais.

## Fases posteriores

- [ ] relatórios;
- [ ] administração completa de usuários/roles/permissões;
- [ ] auditoria completa;
- [ ] backup/restauração;
- [ ] atualização com migration bundle e recuperação segura;
- [ ] instalador;
- [ ] performance/QA;
- [ ] preparação multiusuário;
- [ ] release 1.0.

## Hardening de autenticação — 2026-10-16

O fluxo local agora possui throttling/lockout persistente e mitigação do retorno antecipado por timing. A próxima prioridade de segurança é trilha de auditoria de autenticação e benchmark do custo do hashing em hardware representativo; o smoke visual/interativo do WPF continua obrigatório antes de release.
