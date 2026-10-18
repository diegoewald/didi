# Changelog

Todas as mudanças relevantes serão registradas aqui.

O projeto segue versionamento semântico.

## [Unreleased]

### Added

- primeiro modelo de domínio `Company` e configuração EF Core associada;
- ADR-0005 para persistência exata de valores monetários no SQLite;
- inicialização SQLite com WAL e foreign keys;
- tooling local reproduzível do `dotnet-ef` e design-time factory;
- ADR-0006 definindo migration bundle no fluxo de instalação/atualização;
- usuários locais, autenticação e sessão em memória;
- roles, permissões, FKs e autorização deny-by-default;
- bootstrap atômico e concorrente-seguro do primeiro administrador;
- telas WPF de configuração inicial e login;
- logout com limpeza de sessão e retorno ao login;
- throttling/lockout persistente por conta com bloqueio progressivo;
- dummy password verification para usuário inexistente, inativo ou temporariamente bloqueado;
- migrations `20261016131952_AddAuthenticationThrottling`, `20261016135247_AddSecurityAuditTrail` e `20261017234500_AddAdministrativeAuditContext`;
- entidade `SecurityAuditEvent`, `ISecurityAuditStore` e persistência da trilha de auditoria;
- ferramenta `LocalErp.PasswordHashBenchmark` e script de benchmark reproduzível;
- tela WPF de administração de usuários com criação, status, reset administrativo e associação de roles;
- `SelfDeactivationNotAllowedException` para impedir auto-inativação administrativa;
- `LastAdministrativeRoleAssignmentException` para preservar a última associação efetiva de `roles.manage`;
- `IAdministrativeAuditStore` e `SqliteAdministrativeAuditStore` para mutações administrativas auditadas de forma atômica;
- contexto `ActorUserId` e `RoleId` em `SecurityAuditEvent`;
- `LastAdministrativeRoleCapabilityException` para bloquear desativação/revogação que eliminaria o último `roles.manage` efetivo;
- janela WPF `Roles e permissões` com criação de role, ativação/desativação e administração do `PermissionCatalog`;
- `AccountService` para troca de senha pelo próprio usuário sem exigir permissão administrativa;
- `IUserCredentialStore` e `SqliteUserCredentialStore` com compare-and-set do hash anterior em transação SQLite imediata;
- evento canônico `account.password_changed`;
- janela WPF `Alterar senha` com senha atual, nova senha e confirmação em `PasswordBox`;
- testes de Application e Persistence cobrindo senha atual inválida, política, sessão, conflito de credencial, rollback de auditoria e concorrência entre duas trocas de senha.

### Changed

- removido `Cache=Shared` da conexão SQLite para preparar uso correto de WAL;
- baseline do SDK atualizado para .NET 10 SDK 10.0.402;
- habilitado cross-targeting para validação de build WPF fora do Windows, mantendo release final em Windows;
- migrations de produção deixam de ser responsabilidade do startup normal e passam para o fluxo versionado de atualização;
- startup do desktop roteia explicitamente entre bootstrap, login e shell principal;
- janelas que manipulam credenciais não armazenam senha em ViewModel, sessão, log ou auditoria;
- rehash e reset de falhas de login são concluídos no mesmo fluxo transacional que revalida o estado da conta;
- tentativas feitas durante bloqueio ativo não estendem o bloqueio;
- falha de senha persiste contador e audit event na mesma transação;
- login bem-sucedido persiste reset de falhas, rehash opcional e audit event na mesma transação;
- bootstrap persiste administrador, RBAC e audit event na mesma transação;
- logout limpa a sessão mesmo quando a gravação do evento de auditoria falha;
- navegação do shell carrega assincronamente a área Administração sem mover autorização para a UI;
- `UserService.CreateAsync` e `ChangePasswordAsync` aplicam a mesma `PasswordPolicy` de 15–128 caracteres usada pelo bootstrap;
- `UserService.SetActiveAsync` usa a sessão autenticada para impedir auto-inativação antes da persistência;
- reset administrativo limpa `FailedSignInCount` e `SignInBlockedUntilUtc`, preservando `IsActive`;
- ações de alteração de status e reset de senha são serializadas na View;
- `RoleService` exige `roles.read` para leitura e `roles.manage` para mutações de RBAC;
- novas associações são recusadas para usuários inativos ou roles inativas, enquanto vínculos existentes continuam removíveis;
- a proteção administrativa considera capacidade efetiva — usuário ativo + role ativa + grant — em vez de depender do nome `Administrator`;
- associação/remoção de role e mutações de role/permissão usam transações SQLite imediatas quando a invariável administrativa precisa ser preservada;
- `SecurityAuditEvent` distingue conta afetada, ator administrativo e role alvo sem payload textual arbitrário;
- permission codes auditados são validados pelo catálogo e persistidos somente como `DetailCode` canônico;
- `AccountService.ChangePasswordAsync` exige sessão autenticada e senha atual válida, mas não `users.manage`;
- troca própria passa a revalidar conta ativa e hash esperado dentro do lock de escrita antes de alterar a credencial;
- novo hash, limpeza de lockout e `account.password_changed` são persistidos na mesma transação;
- a sessão local continua válida após a troca própria, enquanto a próxima autenticação exige a nova senha.

### Validation

- Windows Server 2025;
- .NET SDK 10.0.402 / runtime 10.0.13;
- Release build: 0 warnings / 0 errors;
- build de `LocalErp.PasswordHashBenchmark`: 0 warnings / 0 errors;
- suíte automatizada: 127/127;
- Domain 19/19, Application 63/63, Persistence 39/39, Infrastructure 6/6;
- EF Core 10.0.13: sem mudanças pendentes no modelo;
- 6 migrations: apply -> rollback 0 -> reapply passando;
- bootstrap SQLite: rollback e concorrência validados;
- auditoria de autenticação: invariantes, códigos, timestamps UTC, persistência e eventos cobertos;
- administração de usuários Partes A–F validada, incluindo proteção da última associação administrativa e rollback conjunto de auditoria;
- administração de roles/permissões: criação, status, grant/revogação, proteção do último `roles.manage` efetivo e auditoria atômica validados;
- troca própria de senha: run #100 validou build WPF, senha atual, política central, compare-and-set, rollback e concorrência;
- duas trocas concorrentes baseadas no mesmo hash antigo produzem exatamente um commit e um evento;
- benchmark Windows CI com `IterationCount = 220000`: hash mediana 145.155 ms; verify success mediana 138.080 ms; verify failure mediana 141.959 ms;
- smoke visual/interativo do fluxo WPF permanece obrigatório antes de release;
- benchmark em hardware Windows representativo do ambiente final permanece recomendado antes de produção.

## [0.1.0] - 2026-10-15

### Added

- fundação da solução em .NET 10;
- arquitetura em camadas para Domain, Application, Infrastructure, Persistence e Desktop;
- shell WPF inicial;
- estratégia de diretórios locais separados dos binários;
- EF Core 10 + SQLite;
- logging local com Serilog;
- testes iniciais com MSTest;
- documentação de arquitetura, segurança, banco, instalação, atualização, backup e UX;
- ADRs iniciais e backlog central.

## Marco RBAC — 2026-10-16

- usuários locais e password hashing;
- roles, permissions e associações persistidas;
- autorização deny-by-default;
- migration `AddRbacAuthorization` validada;
- CI Windows permanente estabelecido.

## Marco de autenticação utilizável — 2026-10-16

- primeiro acesso cria administrador de forma atômica;
- bootstrap não pode ser executado depois que existe usuário;
- login real bloqueia acesso sem sessão;
- logout retorna ao login;
- fluxo completo compilado e validado pelo CI Windows.

## Marco de hardening de autenticação — 2026-10-16

- falhas de login são contadas por conta e persistidas;
- 3ª falha aplica 2 s, 4ª aplica 10 s e 5ª+ aplica 15 min de bloqueio;
- sucesso limpa contador/bloqueio;
- contas ausentes, inativas e bloqueadas executam dummy password verification;
- estado de throttling e rehash são revalidados transacionalmente antes de concluir o login.

## Marco de auditoria e benchmark — 2026-10-16

- `SecurityAuditEvents` integrado à migration `20261016135247_AddSecurityAuditTrail`;
- eventos de autenticação gravados sem senha, hash, token ou username digitado de conta inexistente;
- atomicidade de auditoria coberta nos fluxos de falha, sucesso e bootstrap;
- logout permanece efetivo mesmo se a persistência da auditoria falhar;
- ferramenta de benchmark integrada e executada no Windows CI;
- baseline automatizado elevado para 82/82 testes e 5 migrations validadas.

## Marco de administração de usuários — Parte A — 2026-10-16

- a área Administração ganhou tela própria de usuários;
- a listagem inclui usuários ativos e inativos;
- `UserService.ListAsync` continua exigindo `users.read` na Application;
- a UI apresenta carregamento, erro e lista vazia;
- dois testes de Application elevaram a suíte para 84 testes.

## Marco de administração de usuários — Parte B — 2026-10-16

- a tela Administração permite criar usuário com confirmação de senha;
- `UserService.CreateAsync` mantém `users.manage` como autorização;
- criação e troca administrativa de senha usam `PasswordPolicy` de 15–128 caracteres;
- senhas ficam fora do ViewModel e são limpas dos `PasswordBox`;
- três testes adicionais elevaram a suíte para 87 testes.

## Marco de administração de usuários — Parte C — 2026-10-17

- a tabela administrativa permite ativar e desativar contas;
- alteração de status permanece protegida por `users.manage`;
- desativação exige confirmação de UI;
- `UserService.SetActiveAsync` impede auto-inativação;
- quatro testes adicionais elevaram a suíte para 91 testes.

## Marco de administração de usuários — Parte D — 2026-10-17

- a tabela administrativa permite redefinir a senha de uma conta;
- `UserService.ChangePasswordAsync` mantém `users.manage` e `PasswordPolicy`;
- nova senha e confirmação ficam fora do ViewModel;
- o reset zera tentativas falhas e bloqueio temporário, preservando o status;
- três testes adicionais elevaram a suíte para 94 testes.

## Marco de administração de usuários — Parte E — 2026-10-17

- a tabela administrativa ganhou ação para gerenciar roles por usuário;
- consulta exige `roles.read`; atribuição e remoção exigem `roles.manage`;
- a remoção da última associação que sustenta `roles.manage` é bloqueada em transação SQLite imediata;
- concorrência dos dois últimos administradores preserva exatamente um caminho administrativo;
- sete testes adicionais elevaram a suíte para 101 testes.

## Marco de administração de usuários — Parte F — 2026-10-17

- `SecurityAuditEvent` passou a suportar `ActorUserId` e `RoleId` opcionais;
- criação, status, reset e associação/remoção de roles geram eventos canônicos;
- `IAdministrativeAuditStore` garante persistência conjunta da mutação e do evento;
- migration `20261017234500_AddAdministrativeAuditContext` preserva histórico e adiciona FKs `SET NULL` para ator/role;
- nove testes adicionais elevaram a suíte para 110 testes.

## Marco de administração de roles/permissões — 2026-10-17

- a área Administração ganhou botão direto para `Roles e permissões`;
- a janela permite criar roles, ativar/desativar e conceder/revogar permissões;
- desativação da última role administrativa e revogação do último `roles.manage` efetivo são bloqueadas;
- mutação e auditoria compartilham a mesma unidade transacional;
- sete testes adicionais elevaram a suíte para 117 testes;
- nenhuma migration nova foi necessária;
- build WPF, snapshot EF e seis migrations permaneceram verdes no CI #98.

## Marco de troca de senha pelo próprio usuário — 2026-10-17

- o shell ganhou a ação `Alterar senha` e uma janela WPF com senha atual, nova senha e confirmação em `PasswordBox`;
- `AccountService` exige sessão autenticada, senha atual válida e `PasswordPolicy`, sem exigir `users.manage`;
- `IUserCredentialStore` revalida conta ativa e hash esperado em transação SQLite imediata antes da gravação;
- reset/troca concorrente não é sobrescrito: mudança do hash esperado provoca conflito e rollback;
- novo hash, limpeza de lockout e `account.password_changed` compartilham a mesma transação;
- falha da auditoria reverte a mudança da credencial;
- teste concorrente confirma exatamente um commit quando duas trocas partem do mesmo hash antigo;
- cinco testes de Application e cinco de Persistence elevaram a suíte para 127 testes;
- nenhuma migration nova foi necessária;
- CI #100 validou build WPF, snapshot EF e ciclo completo das seis migrations.
