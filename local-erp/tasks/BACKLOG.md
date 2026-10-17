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
- [x] Parte E — associar/remover roles de usuários;
- [x] exigir `roles.read` para consultar associações e `roles.manage` para mutá-las;
- [x] impedir novas associações para usuário inativo ou role inativa, mantendo remoção de vínculos existentes;
- [x] proteger atomicamente a remoção da última capacidade efetiva de `roles.manage`;
- [x] validar concorrência de duas remoções administrativas preservando ao menos um administrador efetivo;
- [ ] Parte F — auditar operações administrativas;
- [ ] UI completa de administração de roles/permissões;
- [ ] aplicar salvaguarda equivalente ao expor desativação de role ou revogação de `roles.manage` na UI completa de roles;
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
- [x] salvaguarda contra remoção da última associação que sustenta `roles.manage` efetivo;
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
- [x] `SecurityAuditEvents` persistido com FK opcional e índices de consulta;
- [x] remoção protegida de user-role serializada por transação SQLite imediata.

## P2 — Engenharia

- [x] CI Windows permanente;
- [x] `has-pending-model-changes` no CI;
- [x] build da ferramenta de benchmark no CI;
- [ ] coverage quando a suíte tiver volume suficiente;
- [ ] verificação automatizada de dependências vulneráveis;
- [ ] script de package/release;
- [ ] teste automatizado de UI somente para fluxos cujo custo se justificar.

## Baseline atual — 2026-10-17

O baseline básico de segurança está fechado para evolução funcional, sem esconder as dívidas de pré-release. A administração de usuários possui listagem protegida por `users.read`, criação/status/reset de senha protegidos por `users.manage`, consulta de associações por `roles.read` e mutação de associações por `roles.manage`, sempre com autoridade na camada Application.

A criação e o reset administrativo usam `PasswordBox` diretamente na View para manter senha e confirmação fora do estado e dos métodos do ViewModel. `UserService.CreateAsync` e `ChangePasswordAsync` aplicam a mesma `PasswordPolicy` de 15–128 caracteres antes do hashing.

A Parte C usa `UserService.SetActiveAsync`. Desativação da própria conta autenticada é bloqueada antes de persistir a alteração; a UI pede confirmação antes de desativar e permite reativar contas inativas.

A Parte D expõe `UserService.ChangePasswordAsync` na tabela administrativa. O reset exige confirmação da nova senha, limpa os dois `PasswordBox` após a tentativa e, ao persistir a nova senha, zera `FailedSignInCount` e `SignInBlockedUntilUtc` na mesma atualização. O status ativo/inativo da conta não é alterado e operações de status/reset são serializadas na View.

A Parte E expõe uma janela modal de roles por usuário. `RoleService.ListUserAssignmentsAsync` exige `roles.read`; atribuição e remoção exigem `roles.manage`. Novos vínculos são rejeitados quando usuário ou role está inativo. Ao remover uma associação que sustenta `roles.manage`, `EfRoleRepository` usa uma transação SQLite imediata para verificar e preservar pelo menos um caminho efetivo composto por usuário ativo + role ativa + grant `roles.manage`. O teste concorrente comprova que duas remoções simultâneas dos dois últimos administradores não removem ambos.

A proteção atual cobre a remoção de associação usuário-role, que é a mutação exposta na Parte E. Quando a UI completa de roles/permissões expuser desativação de role ou revogação de `roles.manage`, essas operações deverão receber salvaguarda equivalente antes de serem consideradas prontas.

Baseline automatizado: 101/101 testes (Domain 19, Application 49, Persistence 27, Infrastructure 6), 5 migrations, build Release sem warnings/erros, ferramenta de benchmark compilando, snapshot EF sem mudanças pendentes e ciclo de migrations apply -> rollback 0 -> reapply validado.

Próxima tarefa funcional: Parte F — auditar operações administrativas críticas sem registrar senha, hash ou payload sensível e preservando atomicidade quando a operação alterar estado persistido.
