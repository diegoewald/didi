# Backlog

## P0 — Pré-release / validação de ambiente

- [ ] executar smoke visual/interativo do WPF em Windows real;
- [ ] executar benchmark do password hashing em hardware Windows representativo do ambiente alvo.

## P1 — Administração de usuários

- [x] Parte A — listagem real de usuários ativos e inativos;
- [x] manter `users.read` como autorização da listagem na Application;
- [x] estados loading/erro/vazio da listagem;
- [ ] Parte B — criar usuário com `users.manage`;
- [ ] Parte C — ativar/desativar usuário;
- [ ] Parte D — reset administrativo de senha;
- [ ] Parte E — associar roles;
- [ ] Parte F — auditar operações administrativas;
- [ ] UI de administração de roles/permissões;
- [ ] troca de senha pelo próprio usuário;
- [ ] tela/consulta administrativa da trilha de auditoria quando houver necessidade operacional.

## P1 — Segurança e governança pendente

- [x] password hashing;
- [x] usuários locais;
- [x] sessão autenticada em memória;
- [x] roles e permissões;
- [x] autorização deny-by-default;
- [x] bootstrap seguro do primeiro administrador;
- [x] tela de configuração inicial;
- [x] tela de login;
- [x] logout retornando ao login;
- [x] lockout/throttling persistente por conta;
- [x] mitigação de diferença de timing com dummy password verification;
- [x] trilha de auditoria de autenticação persistida;
- [x] migration `20261016135247_AddSecurityAuditTrail`;
- [x] ferramenta reproduzível de benchmark de password hashing;
- [x] baseline de benchmark executado em Windows CI;
- [x] revisão de segurança final do slice de auditoria/benchmark;
- [ ] definir política de retenção/limpeza de `SecurityAuditEvents` antes de crescimento significativo;
- [ ] avaliar política best-effort/telemetria para falhas de escrita de auditoria nos caminhos de login que não alteram estado;
- [ ] recuperação/reset administrativo de senha com política segura;
- [ ] auditar futuras operações administrativas críticas.

## P1 — Persistência

- [x] tooling local do `dotnet-ef`;
- [x] entidade `Company` e mapping;
- [x] migrations iniciais geradas e validadas;
- [x] WAL e foreign keys;
- [x] testes de migration up/down e constraints;
- [x] seed de desenvolvimento seguro;
- [x] RBAC com FKs reais;
- [x] bootstrap atômico com teste de concorrência;
- [x] estado de throttling persistido com migration e transação SQLite não adiada;
- [x] `SecurityAuditEvents` persistido com FK opcional e índices de consulta.

## P2 — Engenharia

- [x] CI Windows permanente;
- [x] `has-pending-model-changes` no CI;
- [x] build da ferramenta de benchmark no CI;
- [ ] coverage quando a suíte tiver volume suficiente;
- [ ] verificação automatizada de dependências vulneráveis;
- [ ] script de package/release;
- [ ] teste automatizado de UI somente para fluxos cujo custo se justificar.

## Baseline atual — 2026-10-16

O baseline básico de segurança está fechado para evolução funcional, sem esconder as dívidas de pré-release. A Parte A da administração de usuários foi integrada com listagem protegida por `users.read` e sem mover autorização para o WPF.

Baseline automatizado: 84/84 testes, 5 migrations, build Release sem warnings/erros, ferramenta de benchmark compilando, snapshot EF sem mudanças pendentes e ciclo de migrations apply -> rollback 0 -> reapply validado.

Próxima tarefa funcional fechada: Parte B — criar usuário. A implementação deve reutilizar `UserService.CreateAsync`, manter `users.manage` na Application, ler senha diretamente de `PasswordBox`, não colocar senha em ViewModel/log/sessão e não introduzir nova migration sem mudança real de modelo.
