# Local ERP

ERP desktop local-first para Windows, desenvolvido como produto de longo prazo.

## Estado

- Versão: `0.1.0`
- Fase: persistência inicial
- UI: WPF em .NET 10 LTS
- Persistência inicial: SQLite via EF Core 10
- Estratégia futura de rede: PostgreSQL em arquitetura cliente/servidor
- Build/testes: **pendentes enquanto o ambiente atual não possuir .NET 10 SDK**; smoke test/release continuam obrigatoriamente no Windows

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
- Visual Studio 2022/2026 compatível com .NET 10, ou Rider compatível

## Comandos

```powershell
./scripts/build.ps1
./scripts/test.ps1
./scripts/run.ps1
./scripts/new-migration.ps1 InitialCreate
```

Ou diretamente:

```powershell
dotnet restore LocalErp.sln
dotnet build LocalErp.sln -c Release --no-restore
dotnet test LocalErp.sln -c Release --no-build
dotnet run --project src/LocalErp.Desktop/LocalErp.Desktop.csproj
```

## Dados locais

Os binários da aplicação não devem compartilhar diretório com dados do usuário. O shell inicial usa, por padrão:

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
- [Pesquisa técnica inicial](docs/RESEARCH-2026-10-15.md)
- [Especificação mestre](docs/PROJECT-BRIEF.md)
- [Protocolo do agente](SKILL.md)

## RBAC milestone (2026-10-16)

The current security foundation includes local users, password hashing, authenticated in-memory session, roles, permissions and deny-by-default authorization. The validated Windows CI baseline is 53/53 tests with the full EF Core migration cycle passing.

