# Backlog

## P0 — Próxima execução

- [ ] executar smoke visual/interativo do WPF em Windows real;
- [ ] registrar eventos de auditoria de autenticação sem credenciais;
- [ ] benchmarkar custo do password hashing em hardware Windows representativo.

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
- [ ] UI de administração de usuários;
- [ ] UI de roles/permissões;
- [ ] troca de senha pelo próprio usuário;
- [ ] recuperação/reset administrativo de senha com política segura;
- [ ] projetar e implementar trilha de auditoria.

## P1 — Persistência

- [x] tooling local do `dotnet-ef`;
- [x] entidade `Company` e mapping;
- [x] migrations iniciais geradas e validadas;
- [x] WAL e foreign keys;
- [x] testes de migration up/down e constraints;
- [x] seed de desenvolvimento seguro;
- [x] RBAC com FKs reais;
- [x] bootstrap atômico com teste de concorrência;
- [x] estado de throttling persistido com migration e transação SQLite não adiada.

## P2 — Engenharia

- [x] CI Windows permanente;
- [x] `has-pending-model-changes` no CI;
- [ ] coverage quando a suíte tiver volume suficiente;
- [ ] verificação automatizada de dependências vulneráveis;
- [ ] script de package/release;
- [ ] teste automatizado de UI somente para fluxos cujo custo se justificar.

## Hardening de autenticação fechado — 2026-10-16

Baseline: primeiro acesso -> administrador -> sessão -> shell -> logout -> login, agora com throttling/lockout persistente e dummy password verification nos caminhos que antes retornavam cedo.

Próxima prioridade de segurança: auditoria de autenticação + benchmark do hashing. Depois disso, avançar para administração de usuários e cadastros-base sem ampliar dívida de segurança.
