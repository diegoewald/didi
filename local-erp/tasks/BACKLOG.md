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
- [x] Parte F — auditar operações administrativas expostas;
- [x] distinguir ator, usuário alvo e role alvo sem payload textual sensível;
- [x] persistir criação/status/reset e associação/remoção de role junto com o audit event correspondente;
- [x] garantir rollback da mutação quando a persistência da auditoria falha;
- [ ] UI completa de administração de roles/permissões;
- [ ] aplicar salvaguarda equivalente ao expor desativação de role ou revogação de `roles.manage` na UI completa de roles;
- [ ] auditar desativação de role/revogação de permissão quando essas mutações forem expostas;
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
- [x] auditoria atômica das mutações administrativas atualmente expostas;
- [x] migration `20261017234500_AddAdministrativeAuditContext`;
- [ ] definir política de retenção/limpeza de `SecurityAuditEvents` antes de crescimento significativo;
- [ ] avaliar política best-effort/telemetria para falhas de escrita de auditoria nos caminhos de login que não alteram estado;
- [ ] auditar futuras mutações críticas conforme novos casos de uso administrativos forem expostos.

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
- [x] remoção protegida de user-role serializada por transação SQLite imediata;
- [x] contexto administrativo de auditoria com FKs opcionais para ator/role e preservação de histórico;
- [x] testes de rollback comprovando atomicidade entre mutação administrativa e audit event.

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

A Parte E expõe uma janela modal de roles por usuário. `RoleService.ListUserAssignmentsAsync` exige `roles.read`; atribuição e remoção exigem `roles.manage`. Novos vínculos são rejeitados quando usuário ou role está inativo. Ao remover uma associação que sustenta `roles.manage`, a persistência usa uma transação SQLite imediata para verificar e preservar pelo menos um caminho efetivo composto por usuário ativo + role ativa + grant `roles.manage`. O teste concorrente comprova que duas remoções simultâneas dos dois últimos administradores não removem ambos.

A Parte F adiciona contexto administrativo estruturado em `SecurityAuditEvents`: `UserId` representa o usuário afetado, `ActorUserId` identifica o operador e `RoleId` identifica a role quando aplicável. Criação de usuário, alteração de status, reset administrativo e associação/remoção de role são persistidos com seus eventos pela abstração `IAdministrativeAuditStore`. Nos caminhos de user-role a operação usa transação SQLite imediata; nas mutações de `User`, um único `SaveChanges` contém alteração e evento. Testes de persistência comprovam rollback quando a FK do ator da auditoria é inválida.

A migration `20261017234500_AddAdministrativeAuditContext` reconstrói `SecurityAuditEvents` de forma compatível com SQLite, copia o histórico existente e adiciona FKs opcionais `SET NULL` para ator e role. A primeira tentativa intermediária, baseada em `AddForeignKey` sobre tabela existente, foi rejeitada pelo provider SQLite no CI e substituída antes do baseline final.

Baseline automatizado: 110/110 testes (Domain 19, Application 54, Persistence 31, Infrastructure 6), 6 migrations, build Release sem warnings/erros, ferramenta de benchmark compilando, snapshot EF sem mudanças pendentes e ciclo de migrations apply -> rollback 0 -> reapply validado.

Próxima tarefa funcional: administração completa de roles/permissões. Antes de expor desativação de role ou revogação de `roles.manage`, aplicar a mesma salvaguarda de última capacidade administrativa e integrar essas futuras mutações à auditoria atômica.
