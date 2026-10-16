# Local ERP

ERP desktop local-first para Windows, desenvolvido como produto de longo prazo.

## Estado

- Versão: `0.1.0`
- Fase: autenticação local concluída / hardening de segurança
- UI: WPF em .NET 10 LTS
- Persistência: SQLite via EF Core 10
- Estratégia futura de rede: PostgreSQL em arquitetura cliente/servidor
- CI Windows: build Release com 0 warnings / 0 errors e 67/67 testes passando
- Migrations: snapshot consistente e ciclo apply -> rollback 0 -> reapply validado

## Fluxo de acesso atual

1. instalação/banco sem usuários abre a configuração inicial;
2. o primeiro administrador é criado atomicamente, sem credencial padrão;
3. o administrador recebe o papel `Administrator` e todas as permissões registradas;
4. após o bootstrap, a sessão autenticada libera o shell principal;
5. instalações já configuradas abrem a tela de login;
6. logout encerra a sessão e retorna ao login;
7. o `MainWindow` nunca é aberto quando não existe sessão autenticada.

Senhas são tratadas por `PasswordBox`, não entram em ViewModels e não são persistidas na sessão ou em logs.

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

## Segurança — baseline de 2026-10-16

A fundação atual inclui usuários locais, hashing de senha, sessão em memória, bootstrap atômico do primeiro administrador, roles, permissões, autorização deny-by-default, tela de login e logout. O próximo slice de segurança é lockout/throttling, hardening de timing e trilha de auditoria.
