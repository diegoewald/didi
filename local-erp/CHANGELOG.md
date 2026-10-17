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
- tela WPF de configuração inicial sem credencial padrão;
- tela WPF de login com mensagem genérica para credencial inválida;
- logout com limpeza de sessão e retorno ao login;
- identificação do usuário autenticado no shell principal;
- throttling/lockout persistente por conta com bloqueio progressivo;
- dummy password verification para usuário inexistente, inativo ou temporariamente bloqueado;
- migration `20261016131952_AddAuthenticationThrottling` gerada pelo EF Core;
- entidade `SecurityAuditEvent`, abstração `ISecurityAuditStore` e persistência EF Core da trilha de auditoria;
- eventos de auditoria para bootstrap, sucesso/falha/bloqueio de login, throttling e logout;
- migration `20261016135247_AddSecurityAuditTrail` com `SecurityAuditEvents`, FK opcional e índices de consulta;
- testes de domínio, aplicação e persistência para a trilha de auditoria;
- ferramenta `LocalErp.PasswordHashBenchmark` usando o serviço real de hashing do ERP;
- script `scripts/benchmark-password-hashing.ps1` para benchmark reproduzível em Windows;
- tela WPF de administração com listagem de usuários ativos e inativos;
- estados de carregamento, erro e lista vazia para a listagem de usuários;
- testes explícitos para `includeInactive` e exigência de `users.read` em `UserService.ListAsync`;
- formulário WPF de criação de usuário com confirmação por `PasswordBox` e feedback de sucesso/erro;
- testes explícitos para `users.manage` na criação e rejeição de senha abaixo da política em criação e troca administrativa;
- ação WPF para ativar/desativar usuários com confirmação antes da desativação;
- `SelfDeactivationNotAllowedException` para impedir auto-inativação administrativa na Application;
- quatro testes de Application para autorização e mudanças de status de usuário.

### Changed

- removido `Cache=Shared` da conexão SQLite para preparar uso correto de WAL;
- baseline do SDK atualizado para .NET 10 SDK 10.0.402;
- habilitado cross-targeting para validação de build WPF fora do Windows, mantendo release final em Windows;
- migrations de produção deixam de ser responsabilidade do startup normal e passam para o fluxo versionado de atualização;
- startup do desktop agora roteia explicitamente entre bootstrap, login e shell principal;
- janelas de autenticação não armazenam senha em ViewModel, sessão ou log;
- rehash e reset de falhas de login são concluídos no mesmo fluxo transacional que revalida o estado da conta;
- tentativas feitas durante bloqueio ativo não estendem o bloqueio;
- falha de senha persiste contador e audit event na mesma transação;
- login bem-sucedido persiste reset de falhas, rehash opcional e audit event na mesma transação;
- bootstrap persiste administrador, RBAC e audit event na mesma transação;
- logout limpa a sessão mesmo quando a gravação do evento de auditoria falha;
- navegação do shell carrega assincronamente a área Administração sem mover autorização para a UI;
- `UserService.CreateAsync` e `ChangePasswordAsync` passaram a aplicar a mesma `PasswordPolicy` de 15–128 caracteres usada pelo bootstrap;
- a criação administrativa mantém a senha fora do ViewModel: a View lê `PasswordBox`, chama diretamente `UserService` e limpa os campos após a tentativa;
- `UserService.SetActiveAsync` agora usa a sessão autenticada para impedir que o operador desative a própria conta antes da persistência;
- a tabela administrativa atualiza imediatamente o status após ativação/desativação bem-sucedida.

### Validation

- Windows Server 2025;
- .NET SDK 10.0.402 / runtime 10.0.13;
- Release build: 0 warnings / 0 errors;
- build de `LocalErp.PasswordHashBenchmark`: 0 warnings / 0 errors;
- suíte automatizada: 91/91;
- Domain 19/19, Application 42/42, Persistence 24/24, Infrastructure 6/6;
- EF Core 10.0.13: sem mudanças pendentes no modelo;
- 5 migrations: apply -> rollback 0 -> reapply passando;
- migration smoke inclui `SecurityAuditEvents` e a migration `20261016135247_AddSecurityAuditTrail`;
- bootstrap SQLite: rollback e concorrência validados;
- auditoria de autenticação: invariantes, códigos, timestamps UTC, persistência e eventos cobertos;
- administração de usuários Parte A: build WPF e regras de listagem/autorização validados no CI;
- administração de usuários Parte B: criação protegida por `users.manage`, política de senha e fluxo WPF sem senha no ViewModel validados no CI;
- administração de usuários Parte C: ativação/desativação protegida por `users.manage`, auto-inativação bloqueada e UI de status compilada no CI;
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
- contas ausentes, inativas e bloqueadas executam dummy password verification para reduzir diferenças de timing;
- estado de throttling e rehash são revalidados transacionalmente antes de concluir o login.

## Marco de auditoria e benchmark — 2026-10-16

- `SecurityAuditEvents` integrado ao modelo e à migration `20261016135247_AddSecurityAuditTrail`;
- eventos de autenticação gravados sem senha, hash, token ou username digitado de conta inexistente;
- atomicidade de auditoria coberta nos fluxos de falha, sucesso e bootstrap;
- logout permanece efetivo mesmo se a persistência da auditoria falhar;
- ferramenta de benchmark integrada ao repositório e compilada pelo CI;
- benchmark executado em Windows Server 2025 com o serviço real do ERP;
- baseline automatizado elevado para 82/82 testes e 5 migrations validadas.

## Marco de administração de usuários — Parte A — 2026-10-16

- a área Administração ganhou tela própria de usuários;
- a listagem inclui usuários ativos e inativos;
- `UserService.ListAsync` continua exigindo `users.read` na Application;
- a UI apresenta carregamento, erro e lista vazia sem usar code-behind para regra de negócio;
- dois testes de Application elevaram a suíte para 84 testes.

## Marco de administração de usuários — Parte B — 2026-10-16

- a tela Administração permite criar usuário com confirmação de senha;
- `UserService.CreateAsync` mantém `users.manage` como autorização na Application;
- criação e troca administrativa de senha usam `PasswordPolicy` de 15–128 caracteres;
- senhas ficam fora do ViewModel e são limpas dos `PasswordBox` após cada tentativa;
- três testes adicionais de Application elevaram a suíte para 87 testes;
- nenhuma migration nova foi necessária.

## Marco de administração de usuários — Parte C — 2026-10-17

- a tabela administrativa permite ativar e desativar contas;
- alteração de status permanece protegida por `users.manage` na Application;
- desativação exige confirmação de UI;
- `UserService.SetActiveAsync` impede auto-inativação antes de persistir;
- quatro testes adicionais de Application elevaram a suíte para 91 testes;
- nenhuma migration nova foi necessária.
