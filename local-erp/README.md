# Local ERP

ERP desktop local-first para Windows, desenvolvido como produto de longo prazo.

## Estado

- Versão: `0.1.0`
- Fase: baseline básico de segurança fechado / administração de usuários, roles e conta operacional
- UI: WPF em .NET 10 LTS
- Persistência: SQLite via EF Core 10
- Estratégia futura de rede: PostgreSQL em arquitetura cliente/servidor
- CI Windows: build Release com 0 warnings / 0 errors e 127/127 testes passando
- Migrations: 6 migrations, snapshot consistente e ciclo apply -> rollback 0 -> reapply validado

## Fluxo de acesso atual

1. instalação/banco sem usuários abre a configuração inicial;
2. o primeiro administrador é criado atomicamente, sem credencial padrão;
3. o administrador recebe a role `Administrator` e todas as permissões registradas;
4. após o bootstrap, a sessão autenticada libera o shell principal;
5. instalações já configuradas abrem a tela de login;
6. falhas de autenticação são limitadas por conta com throttling/lockout persistente;
7. usuário inexistente, inativo ou bloqueado executa dummy password verification para reduzir diferença de timing;
8. eventos críticos de autenticação são registrados em `SecurityAuditEvents` sem senha, hash, token ou username digitado de conta inexistente;
9. logout limpa a sessão e tenta registrar o evento de auditoria sem manter a sessão caso essa gravação falhe;
10. o `MainWindow` nunca é aberto quando não existe sessão autenticada;
11. a área Administração lista usuários ativos/inativos por `users.read` e permite criar, ativar/desativar e redefinir senha por `users.manage`;
12. a conta autenticada não pode desativar a si própria;
13. reset administrativo de senha remove tentativas falhas e bloqueio temporário, mas preserva o status ativo/inativo da conta;
14. a tabela permite abrir as roles de cada usuário; consulta exige `roles.read` e associação/remoção exige `roles.manage`;
15. remover uma associação que eliminaria o último usuário ativo com capacidade efetiva de `roles.manage` é bloqueado atomicamente no SQLite;
16. criação de usuário, ativação/desativação, reset administrativo e associação/remoção de roles geram auditoria administrativa identificando ator e alvo;
17. a Administração possui janela própria para criar roles, ativar/desativar e conceder/revogar permissões do catálogo;
18. desativar uma role ou revogar `roles.manage` é bloqueado quando eliminaria a última capacidade efetiva de administrar roles;
19. mutações administrativas auditadas e seus eventos são persistidos atomicamente: falha de auditoria reverte a mudança de estado;
20. qualquer usuário autenticado pode abrir `Alterar senha`, confirmar a senha atual e definir uma nova senha conforme a política central;
21. a troca própria usa compare-and-set do hash dentro de transação SQLite imediata, impedindo sobrescrever silenciosamente reset/troca concorrente;
22. novo hash, limpeza de lockout e `account.password_changed` são commitados juntos.

Senhas da UI são lidas diretamente de `PasswordBox`, não são armazenadas no ViewModel, sessão, logs ou auditoria e os campos protegidos são limpos após a tentativa. Bootstrap, criação administrativa, reset e troca própria usam a política central de 15 a 128 caracteres antes do hashing.

## Administração de usuários, roles e conta

As Partes A, B, C, D, E e F da Administração de usuários estão concluídas. A tela WPF carrega usuários por `UserService.ListAsync(includeInactive: true)`, apresenta estados de carregamento, erro e lista vazia, permite criar contas, gerenciar status ativo/inativo, redefinir senhas e abrir a associação de roles de cada usuário.

A UI não substitui autorização: a listagem exige `users.read`; criação, alteração de status e reset administrativo de senha exigem `users.manage`; leitura das associações exige `roles.read`; atribuição e remoção de roles exigem `roles.manage` na camada Application.

Desativar uma conta exige confirmação visual. `UserService.SetActiveAsync` bloqueia a auto-inativação da conta da sessão antes de persistir a alteração. No reset administrativo, `UserService.ChangePasswordAsync` aplica `PasswordPolicy`, troca o hash e limpa `FailedSignInCount`/`SignInBlockedUntilUtc` na mesma atualização, sem alterar `IsActive`.

`RoleService.ListUserAssignmentsAsync` projeta roles ativas e inativas com o estado de associação. Contas inativas e roles inativas não recebem novas associações, mas vínculos existentes podem ser removidos. A remoção protegida usa transação SQLite imediata e rejeita a operação quando não restaria nenhum caminho efetivo formado por usuário ativo + role ativa + grant `roles.manage`.

A janela `Roles e permissões` permite criar roles, alterar seu status e administrar todas as permissões de `PermissionCatalog`. `RoleService.SetActiveAsync` e `RevokePermissionAsync` usam o contrato transacional de auditoria. Quando uma desativação ou revogação de `roles.manage` removeria o último caminho efetivo de administração, a operação é revertida e a Application retorna `LastAdministrativeRoleCapabilityException`.

A troca própria de senha é tratada por `AccountService`, separada do reset administrativo e sem exigir `users.manage`. A senha atual precisa ser verificada primeiro. `IUserCredentialStore` revalida conta ativa e hash esperado em transação SQLite imediata antes de substituir a credencial. Se uma operação concorrente já tiver alterado o hash, a troca falha sem sobrescrever o estado mais recente. O evento `account.password_changed` usa a própria conta como `UserId` e `ActorUserId`, sem senha, hash ou payload textual.

`SecurityAuditEvent` distingue `UserId` alvo, `ActorUserId` e `RoleId` opcional. Para grants/revogações de permissão, `DetailCode` recebe somente o código canônico conhecido do catálogo. Não há payload textual arbitrário. Mutações auditadas relevantes são commitadas junto com seus eventos; no-op, conflito de credencial e operação bloqueada não produzem falso evento de sucesso.

A migration `20261017234500_AddAdministrativeAuditContext` preserva os eventos anteriores ao reconstruir `SecurityAuditEvents` de forma compatível com SQLite e adicionar as FKs de ator/role com `SET NULL`. Administração de roles/permissões e troca própria de senha não exigiram migrations adicionais.

Próxima frente funcional: Fase 6 — tornar empresa/filial operacional na UI sobre `CompanyService` e persistência já existentes. Também permanecem no backlog a consulta operacional da trilha de auditoria e sua política de retenção.

## Política de tentativas de login

- 1ª e 2ª falhas: sem bloqueio temporal;
- 3ª falha: 2 segundos;
- 4ª falha: 10 segundos;
- 5ª falha e seguintes: 15 minutos;
- sucesso limpa o estado de falhas;
- reset administrativo e troca própria bem-sucedida também limpam contador/bloqueio temporário;
- tentativas durante bloqueio ativo não prolongam o bloqueio.

## Objetivos de engenharia

- funcionar sem internet para operações principais;
- separar regras de negócio, persistência e interface;
- preservar dados entre atualização/desinstalação;
- suportar permissões e auditoria na camada de aplicação;
- evoluir de instalação em uma máquina para cenário multiusuário sem reescrever o domínio;
- manter migrations, testes, logs, backup/restauração e atualização como partes do produto.

## Estrutura

```text
src/
  LocalErp.Domain/
  LocalErp.Application/
  LocalErp.Infrastructure/
  LocalErp.Persistence/
  LocalErp.Desktop/
tests/
  LocalErp.Domain.Tests/
  LocalErp.Application.Tests/
  LocalErp.Infrastructure.Tests/
  LocalErp.Persistence.Tests/
tools/
  LocalErp.PasswordHashBenchmark/
docs/
  adr/
tasks/
scripts/
```

## Pré-requisitos de desenvolvimento

- Windows 10/11 x64 para execução e release final
- .NET 10 SDK (LTS), baseline atual `10.0.402`
- PowerShell 7 recomendado
- Visual Studio compatível com .NET 10, ou Rider compatível

## Comandos

```powershell
./scripts/build.ps1
./scripts/test.ps1
./scripts/run.ps1
./scripts/new-migration.ps1 NomeDaMigration
./scripts/benchmark-password-hashing.ps1
```

Ou diretamente:

```powershell
dotnet restore LocalErp.sln
dotnet build LocalErp.sln -c Release --no-restore
dotnet test LocalErp.sln -c Release --no-build
dotnet run --project src/LocalErp.Desktop/LocalErp.Desktop.csproj
```

## Dados locais

Os binários da aplicação não devem compartilhar diretório com dados do usuário. O shell usa, por padrão:

```text
%LocalAppData%\LocalErp\
  Data\
  Logs\
  Backups\
  Attachments\
```

Banco, logs, anexos e backups reais nunca devem ser versionados.

## Documentação

- [Arquitetura](docs/ARCHITECTURE.md)
- [Roadmap](docs/ROADMAP.md)
- [Banco de dados](docs/DATABASE.md)
- [Segurança](docs/SECURITY.md)
- [Testes](docs/TESTING.md)
- [Instalação](docs/INSTALLATION.md)
- [Atualização](docs/UPDATE-STRATEGY.md)
- [Backup e restauração](docs/BACKUP-RESTORE.md)
- [Diretrizes de UX](docs/UX-GUIDELINES.md)
- [Especificação mestre](docs/PROJECT-BRIEF.md)
- [Protocolo do agente](SKILL.md)

## Segurança — baseline de 2026-10-17

A fundação atual inclui usuários locais, hashing de senha, política única de senha, sessão em memória, bootstrap atômico do primeiro administrador, roles, permissões, autorização deny-by-default, login/logout, throttling/lockout persistente, mitigação de timing por dummy password verification, trilha de auditoria de autenticação, bloqueio de auto-inativação administrativa, recuperação administrativa de senha, associação segura de roles, administração completa de roles/permissões, troca própria de senha e auditoria atômica das mutações relevantes.

As migrations `20261016135247_AddSecurityAuditTrail` e `20261017234500_AddAdministrativeAuditContext` estão integradas e validadas. O benchmark reproduzível do hashing foi executado no Windows Server 2025 do CI usando o serviço real do ERP e `IterationCount = 220000`; a mediana observada ficou em aproximadamente 145 ms para hash, 138 ms para verify válido e 142 ms para verify inválido.

Antes de release de produção ainda é recomendado executar o mesmo benchmark em hardware Windows representativo do ambiente alvo. O smoke visual/interativo do fluxo WPF em Windows real também continua obrigatório. A política de retenção/limpeza de `SecurityAuditEvents` ainda deve ser definida antes de crescimento significativo da base de auditoria.
