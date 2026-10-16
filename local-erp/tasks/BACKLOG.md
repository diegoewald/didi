# Backlog

## P0 — Próxima execução

- [ ] executar smoke visual/interativo do WPF em Windows real;
- [ ] executar benchmark do password hashing em hardware Windows representativo do ambiente alvo.

## P1 — Segurança e administração

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
- [ ] UI de administração de usuários;
- [ ] UI de roles/permissões;
- [ ] troca de senha pelo próprio usuário;
- [ ] recuperação/reset administrativo de senha com política segura;
- [ ] tela/consulta administrativa da trilha de auditoria quando houver necessidade operacional;
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

## Baseline básico de segurança fechado — 2026-10-16

A revisão do slice de auditoria confirmou atomicidade nos fluxos que alteram estado, ausência de credenciais/payload arbitrário no modelo de auditoria, preservação da sessão encerrada mesmo quando a auditoria de logout falha e schema/migration coerentes. Não foi encontrado defeito bloqueante para iniciar administração de usuários.

A trilha de autenticação e o benchmark reproduzível estão integrados. Baseline atual: 82/82 testes, 5 migrations, build Release sem warnings/erros e ciclo de migrations apply -> rollback 0 -> reapply validado.

O benchmark já foi executado em Windows CI, mas isso não fecha a validação de hardware final. Antes de release, executar o mesmo benchmark numa máquina representativa e manter o smoke visual/interativo do WPF como obrigatório.

Próxima frente funcional: administração de usuários em partes pequenas, começando pela listagem real protegida por `users.read`. Depois, criação, ativação/desativação, reset administrativo de senha, associação de roles e auditoria das operações administrativas.
