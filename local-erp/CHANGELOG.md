# Changelog

Todas as mudanças relevantes serão registradas aqui.

O projeto segue versionamento semântico.

## [Unreleased]

### Added

- primeiro modelo de domínio `Company` e configuração EF Core associada;
- ADR-0005 para persistência exata de valores monetários no SQLite;
- inicialização SQLite que habilita/valida WAL e foreign keys;
- tooling local reproduzível do `dotnet-ef` e design-time factory;
- ADR-0006 definindo migration bundle no fluxo de instalação/atualização.

### Changed

- removido `Cache=Shared` da conexão SQLite para preparar uso correto de WAL;
- baseline do SDK atualizado para .NET 10 SDK 10.0.402;
- habilitado cross-targeting para validação de build WPF fora do Windows, mantendo release final em Windows;
- migrations de produção deixam de ser responsabilidade do startup normal da aplicação e passam para o fluxo versionado de atualização.

## [0.1.0] - 2026-10-15

### Added

- fundação da solução em .NET 10;
- arquitetura em camadas para Domain, Application, Infrastructure, Persistence e Desktop;
- shell WPF inicial;
- estratégia de diretórios locais separados dos binários;
- EF Core 10 + SQLite preparado para a fase de persistência;
- logging local com Serilog e retenção configurada;
- testes iniciais com MSTest;
- documentação de arquitetura, segurança, banco, instalação, atualização, backup e UX;
- ADRs iniciais e backlog central.

### Validation

- build: não executado nesta sessão; ambiente disponível não possui .NET SDK;
- testes: não executados nesta sessão pelo mesmo motivo.

## RBAC baseline validated — 2026-10-16

- Local users, password hashing, in-memory session and role-based authorization are implemented.
- Roles map to permissions through `RolePermissions`; users map to roles through `UserRoles`.
- Authorization is deny-by-default for protected application operations.
- EF Core migration `AddRbacAuthorization` is validated against the current model.
- Windows CI baseline: Release build with 0 warnings / 0 errors and 53/53 tests passing.
- Migration validation: no pending model changes, apply -> rollback to 0 -> reapply passing.

