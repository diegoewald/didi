# Backlog

## P0 — Pré-release / validação de ambiente

- [ ] executar smoke visual/interativo do WPF em Windows real;
- [ ] executar benchmark do password hashing em hardware Windows representativo do ambiente alvo.

## P1 — Administração de usuários e roles

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
- [x] UI completa de administração de roles/permissões;
- [x] permitir criação e ativação/desativação de roles pela UI protegida por `roles.manage`;
- [x] permitir concessão/revogação de permissões do `PermissionCatalog`;
- [x] proteger desativação de role quando eliminaria o último `roles.manage` efetivo;
- [x] proteger revogação de `roles.manage` quando eliminaria a última capacidade administrativa efetiva;
- [x] auditar atomicamente criação/status de role e grant/revogação de permissões;
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
- [x] auditoria atômica das mutações administrativas expostas;
- [x] migration `20261017234500_AddAdministrativeAuditContext`;
- [x] salvaguarda equivalente para desativação de role e revogação de `roles.manage`;
- [x] auditoria atômica de criação/status de role e grant/revogação de permissões;
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
- [x] testes de rollback comprovando atomicidade entre mutação administrativa e audit event;
- [x] desativação de role/revogação de `roles.manage` protegidas em transação SQLite imediata.

## P2 — Engenharia

- [x] CI Windows permanente;
- [x] `has-pending-model-changes` no CI;
- [x] build da ferramenta de benchmark no CI;
- [ ] coverage quando a suíte tiver volume suficiente;
- [ ] verificação automatizada de dependências vulneráveis;
- [ ] script de package/release;
- [ ] teste automatizado de UI somente para fluxos cujo custo se justificar.

## Baseline atual — 2026-10-17

O baseline básico de segurança está fechado para evolução funcional, sem esconder as dívidas de pré-release. A administração possui listagem de usuários protegida por `users.read`, mutações de usuário por `users.manage` e administração de associações, roles e permissões por `roles.read`/`roles.manage`, sempre com autoridade na Application e nas invariantes persistentes.

A criação e o reset administrativo usam `PasswordBox` diretamente na View para manter senha e confirmação fora do estado e dos métodos do ViewModel. `UserService.CreateAsync` e `ChangePasswordAsync` aplicam a mesma `PasswordPolicy` de 15–128 caracteres antes do hashing.

A Parte E protege a remoção de associação usuário-role. Quando o vínculo sustenta `roles.manage`, a persistência usa transação SQLite imediata para preservar pelo menos um caminho efetivo composto por usuário ativo + role ativa + grant `roles.manage`.

A Parte F adiciona contexto administrativo estruturado em `SecurityAuditEvents`: `UserId` representa o usuário afetado, `ActorUserId` identifica o operador e `RoleId` identifica a role quando aplicável. Mutações administrativas e eventos são persistidos como unidade atômica, com rollback conjunto em falha.

A administração completa de roles/permissões amplia a mesma regra. `RoleService.CreateAsync`, `SetActiveAsync`, `GrantPermissionAsync` e `RevokePermissionAsync` passam pelo `IAdministrativeAuditStore`. Desativação de uma role que contribui para `roles.manage` e revogação desse grant são avaliadas dentro de transação SQLite imediata; se não restar outro caminho efetivo, a operação é bloqueada sem alterar estado nem registrar falso evento de sucesso. A janela WPF expõe criação, status e as seis permissões atuais do catálogo, com confirmação para reduções de privilégio.

Baseline automatizado: 117/117 testes (Domain 19, Application 58, Persistence 34, Infrastructure 6), 6 migrations, build Release sem warnings/erros, ferramenta de benchmark compilando, snapshot EF sem mudanças pendentes e ciclo de migrations apply -> rollback 0 -> reapply validado.

Próxima tarefa funcional: troca de senha pelo próprio usuário, preservando a política central e o tratamento seguro de senha fora do ViewModel. A consulta/retensão operacional da trilha de auditoria permanece depois disso.
