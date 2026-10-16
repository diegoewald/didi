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
- identificação do usuário autenticado no shell principal.

### Changed

- removido `Cache=Shared` da conexão SQLite para preparar uso correto de WAL;
- baseline do SDK atualizado para .NET 10 SDK 10.0.402;
- habilitado cross-targeting para validação de build WPF fora do Windows, mantendo release final em Windows;
- migrations de produção deixam de ser responsabilidade do startup normal e passam para o fluxo versionado de atualização;
- startup do desktop agora roteia explicitamente entre bootstrap, login e shell principal;
- janelas de autenticação não armazenam senha em ViewModel, sessão ou log.

### Validation

- Windows Server 2025;
- .NET SDK 10.0.402 / runtime 10.0.13;
- Release build: 0 warnings / 0 errors;
- suíte automatizada: 67/67 antes do fechamento visual do Desktop;
- EF Core 10.0.13: sem mudanças pendentes no modelo;
- migrations: apply -> rollback 0 -> reapply passando;
- bootstrap SQLite: rollback e concorrência validados;
- smoke visual/interativo do fluxo WPF permanece obrigatório antes de release.

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
