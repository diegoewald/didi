# Backlog

## P0 — Pré-release / validação de ambiente

- [ ] executar smoke visual/interativo do WPF em Windows real;
- [ ] executar benchmark do password hashing em hardware Windows representativo do ambiente alvo.

## P1 — Administração de usuários

- [x] Parte A — listagem real de usuários ativos e inativos;
- [x] manter `users.read` como autorização da listagem na Application;
- [x] estados loading/erro/vazio da listagem;
- [x] Parte B — criar usuário com `users.manage`;
- [x] aplicar `PasswordPolicy` de 15–128 caracteres em criação e troca administrativa de senha;
- [x] manter senha fora de propriedades/métodos do ViewModel: `PasswordBox` envia diretamente para `UserService` e é limpo após a tentativa;
- [x] Parte C — ativar/desativar usuário com `users.manage`;
- [x] bloquear auto-inativação da conta autenticada na camada Application;
- [x] exigir confirmação visual antes de desativar outra conta;
- [x] Parte D — reset administrativo de senha com `users.manage`;
- [x] reset administrativo limpa tentativas falhas/bloqueio temporário sem alterar status ativo/inativo;
- [x] senha do reset permanece somente em `PasswordBox`/parâmetro transitório e é limpa após a tentativa;
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
- [x] recuperação/reset administrativo de senha com política segura;
- [ ] definir política de retenção/limpeza de `SecurityAuditEvents` antes de crescimento significativo;
- [ ] avaliar política best-effort/telemetria para falhas de escrita de auditoria nos caminhos de login que não alteram estado;
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

## Baseline atual — 2026-10-17

O baseline básico de segurança está fechado para evolução funcional, sem esconder as dívidas de pré-release. A administração de usuários possui listagem protegida por `users.read` e criação/status/reset de senha protegidos por `users.manage`, sempre na camada Application.

A criação e o reset administrativo usam `PasswordBox` diretamente na View para manter senha e confirmação fora do estado e dos métodos do ViewModel. `UserService.CreateAsync` e `ChangePasswordAsync` aplicam a mesma `PasswordPolicy` de 15–128 caracteres antes do hashing.

A Parte C usa `UserService.SetActiveAsync`. Desativação da própria conta autenticada é bloqueada antes de persistir a alteração; portanto, toda desativação autorizada de outra conta preserva o operador atual, que já precisou passar por `users.manage`. A UI pede confirmação antes de desativar e permite reativar contas inativas. Salvaguardas relacionadas à remoção da última role administrativa serão tratadas na Parte E, quando essa mutação existir.

A Parte D expõe `UserService.ChangePasswordAsync` na tabela administrativa. O reset exige confirmação da nova senha, limpa os dois `PasswordBox` após a tentativa e, ao persistir a nova senha, zera `FailedSignInCount` e `SignInBlockedUntilUtc` na mesma atualização. O status ativo/inativo da conta não é alterado e operações de status/reset são serializadas na View para evitar ações concorrentes sobre a mesma tela.

Baseline automatizado: 94/94 testes (Domain 19, Application 45, Persistence 24, Infrastructure 6), 5 migrations, build Release sem warnings/erros, ferramenta de benchmark compilando, snapshot EF sem mudanças pendentes e ciclo de migrations apply -> rollback 0 -> reapply validado.

Próxima tarefa funcional: Parte E — associar roles. Antes de permitir remoção de roles/capacidades administrativas, implementar salvaguarda na Application contra remoção da última capacidade administrativa efetiva.
