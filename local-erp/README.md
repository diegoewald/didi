# Local ERP

ERP desktop local-first para Windows, desenvolvido como produto de longo prazo.

## Estado

- Versão: `0.1.0`
- Fase: baseline básico de segurança fechado / administração de usuários em andamento
- UI: WPF em .NET 10 LTS
- Persistência: SQLite via EF Core 10
- Estratégia futura de rede: PostgreSQL em arquitetura cliente/servidor
- CI Windows: build Release com 0 warnings / 0 errors e 91/91 testes passando
- Migrations: 5 migrations, snapshot consistente e ciclo apply -> rollback 0 -> reapply validado

## Fluxo de acesso atual

1. instalação/banco sem usuários abre a configuração inicial;
2. o primeiro administrador é criado atomicamente, sem credencial padrão;
3. o administrador recebe o papel `Administrator` e todas as permissões registradas;
4. após o bootstrap, a sessão autenticada libera o shell principal;
5. instalações já configuradas abrem a tela de login;
6. falhas de autenticação são limitadas por conta com throttling/lockout persistente;
7. usuário inexistente, inativo ou bloqueado executa dummy password verification para reduzir diferença de timing;
8. eventos críticos de autenticação são registrados em `SecurityAuditEvents` sem senha, hash, token ou username digitado de conta inexistente;
9. logout limpa a sessão e tenta registrar o evento de auditoria sem manter a sessão caso essa gravação falhe;
10. o `MainWindow` nunca é aberto quando não existe sessão autenticada;
11. a área Administração lista usuários ativos/inativos por `users.read`, permite criar contas por `users.manage` e também ativar/desativar outras contas por `users.manage`;
12. a conta autenticada não pode desativar a si própria.

Senhas da UI são lidas diretamente de `PasswordBox`, não são armazenadas no ViewModel, sessão, logs ou auditoria e os campos protegidos são limpos após a tentativa. Criação e troca administrativa de senha usam a política central de 15 a 128 caracteres antes do hashing.

## Administração de usuários

As Partes A, B e C estão concluídas. A tela WPF de Administração carrega usuários por `UserService.ListAsync(includeInactive: true)`, apresenta estados de carregamento, erro e lista vazia, permite criar uma conta nova e gerenciar o status ativo/inativo das contas.

A UI não substitui autorização: a listagem exige `users.read`; criação e alteração de status exigem `users.manage` na camada Application. A senha da criação não passa pelo ViewModel; o code-behind limita-se à captura/limpeza dos `PasswordBox`, confirmação/feedback de apresentação e chamada do `UserService`.

Desativar uma conta exige confirmação visual. `UserService.SetActiveAsync` bloqueia a auto-inativação da conta da sessão antes de persistir a alteração. Como o operador precisa estar autenticado e autorizado com `users.manage`, a desativação válida de outra conta preserva o operador atual. Salvaguardas para remoção da última role administrativa serão tratadas junto da associação de roles, quando essa mutação existir.

Próximas partes planejadas: reset administrativo de senha, associação de roles e auditoria das operações administrativas.

## Política de tentativas de login

- 1ª e 2ª falhas: sem bloqueio temporal;
- 3ª falha: 2 segundos;
- 4ª falha: 10 segundos;
- 5ª falha e seguintes: 15 minutos;
- sucesso limpa o estado de falhas;
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

A fundação atual inclui usuários locais, hashing de senha, política única de senha, sessão em memória, bootstrap atômico do primeiro administrador, roles, permissões, autorização deny-by-default, login/logout, throttling/lockout persistente, mitigação de timing por dummy password verification, trilha de auditoria de autenticação persistida por EF Core/SQLite e bloqueio de auto-inativação administrativa.

A migration `20261016135247_AddSecurityAuditTrail` está integrada e validada. O benchmark reproduzível do hashing também foi executado no Windows Server 2025 do CI usando o serviço real do ERP e `IterationCount = 220000`; a mediana observada ficou em aproximadamente 145 ms para hash, 138 ms para verify válido e 142 ms para verify inválido.

Antes de release de produção ainda é recomendado executar o mesmo benchmark em hardware Windows representativo do ambiente alvo. O smoke visual/interativo do fluxo WPF em Windows real também continua obrigatório. A política de retenção/limpeza de `SecurityAuditEvents` ainda deve ser definida antes de crescimento significativo da base de auditoria.
