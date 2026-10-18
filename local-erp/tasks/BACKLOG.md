# Backlog

## P0 — Pré-release / validação de ambiente

- [ ] executar smoke visual/interativo do WPF em Windows real;
- [ ] executar benchmark do password hashing em hardware Windows representativo do ambiente alvo.

## P1 — Administração de usuários e roles

- [x] Parte A — listagem real de usuários ativos e inativos;
- [x] manter `users.read` como autorização da listagem na Application;
- [x] estados loading/erro/vazio da listagem;
- [x] Parte B — criar usuário com `users.manage`;
- [x] aplicar `PasswordPolicy` de 15–128 caracteres em criação, reset administrativo e troca própria de senha;
- [x] manter senhas fora de propriedades/métodos de ViewModel: `PasswordBox` envia diretamente para a camada Application e é limpo após a tentativa;
- [x] Parte C — ativar/desativar usuário com `users.manage`;
- [x] bloquear auto-inativação da conta autenticada na camada Application;
- [x] exigir confirmação visual antes de desativar outra conta;
- [x] Parte D — reset administrativo de senha com `users.manage`;
- [x] reset administrativo limpa tentativas falhas/bloqueio temporário sem alterar status ativo/inativo;
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
- [x] troca de senha pelo próprio usuário;
- [x] exigir senha atual válida antes de trocar a própria senha;
- [x] revalidar hash esperado e conta ativa em transação SQLite imediata antes do commit;
- [x] persistir novo hash, limpeza de lockout e `account.password_changed` na mesma transação;
- [x] impedir que duas trocas concorrentes baseadas no mesmo hash antigo sejam commitadas;
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
- [x] troca própria de senha com senha atual, compare-and-set e auditoria atômica;
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
- [x] desativação de role/revogação de `roles.manage` protegidas em transação SQLite imediata;
- [x] troca própria de senha usa transação SQLite imediata e compare-and-set do hash anterior.

## P1 — Cadastros-base

- [x] tornar `Company` operacional em `Cadastros` com listagem de ativos/inativos;
- [x] criar empresa pela UI usando `companies.manage` na Application;
- [x] renomear e ativar/desativar empresa sem mover autorização para o WPF;
- [x] estados loading/erro/vazio e refresh explícito para o catálogo de empresas;
- [x] testes explícitos de `companies.read` para listagem e `companies.manage` para mutações;
- [ ] definir e modelar `Branch`/filial vinculada à empresa antes de expor a UI de filiais;
- [ ] manter dados fiscais/endereço de filial fora do modelo até o requisito de negócio ser definido;
- [ ] fornecedores;
- [ ] produtos/materiais;
- [ ] unidades/categorias;
- [ ] centros de custo.

## P2 — Engenharia

- [x] CI Windows permanente;
- [x] `has-pending-model-changes` no CI;
- [x] build da ferramenta de benchmark no CI;
- [ ] coverage quando a suíte tiver volume suficiente;
- [ ] verificação automatizada de dependências vulneráveis;
- [ ] script de package/release;
- [ ] teste automatizado de UI somente para fluxos cujo custo se justificar.

## Baseline atual — 2026-10-17

O baseline básico de segurança está fechado para evolução funcional, sem esconder as dívidas de pré-release. A administração possui listagem de usuários protegida por `users.read`, mutações administrativas de usuário por `users.manage` e administração de associações, roles e permissões por `roles.read`/`roles.manage`, sempre com autoridade na Application e nas invariantes persistentes.

A troca de senha pelo próprio usuário é um caso de uso separado do reset administrativo. `AccountService.ChangePasswordAsync` exige sessão autenticada, valida a senha atual, aplica `PasswordPolicy` à nova senha e não exige `users.manage`. `IUserCredentialStore` revalida conta ativa e hash esperado dentro de uma transação SQLite imediata; assim um reset administrativo ou outra troca concorrente não pode ser sobrescrito silenciosamente. Novo hash, limpeza do estado de falhas e evento `account.password_changed` são commitados juntos.

`Cadastros` agora possui uma tela operacional de empresas sobre o `CompanyService` já existente. A lista inclui empresas ativas e inativas e oferece criação, renomeação e ativação/desativação. `companies.read` continua sendo exigida para consulta e `companies.manage` para todas as mutações na Application. Nenhuma regra de autorização foi deslocada para a UI e nenhuma migration nova foi necessária.

Baseline automatizado: 129/129 testes (Domain 19, Application 65, Persistence 39, Infrastructure 6), 6 migrations, build Release sem warnings/erros, ferramenta de benchmark compilando, snapshot EF sem mudanças pendentes e ciclo de migrations apply -> rollback 0 -> reapply validado.

Próxima tarefa funcional: completar empresa/filial modelando uma entidade de filial vinculada a `Company` antes de criar sua UI. Dados fiscais, endereço e demais campos específicos não devem ser inventados sem requisito de negócio. A consulta/retenção operacional da trilha de auditoria continua no roadmap posterior; smoke WPF e benchmark em hardware representativo permanecem pendências de pré-release.
