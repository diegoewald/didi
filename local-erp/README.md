# Local ERP

ERP desktop local-first para Windows, desenvolvido como produto de longo prazo.

## Estado

- Versão: `0.1.0`
- Fase: segurança/administração baseline fechado; Fase 6 de cadastros-base em andamento
- UI: WPF em .NET 10 LTS
- Persistência: SQLite via EF Core 10
- Estratégia futura de rede: PostgreSQL em arquitetura cliente/servidor
- CI Windows: build Release com 0 warnings / 0 errors e 148/148 testes passando
- Migrations: 8 migrations, snapshot consistente e ciclo apply -> rollback 0 -> reapply validado

## Fluxo de acesso, segurança e robustez

O primeiro acesso cria o administrador atomicamente e sem credencial padrão. Instalações configuradas exigem login; a sessão autenticada libera o shell e logout retorna ao login. Autenticação possui throttling/lockout persistente, dummy password verification para reduzir diferença de timing e trilha `SecurityAuditEvents` sem senha, hash, token ou username digitado de conta inexistente.

A Administração permite gerenciar usuários, roles e permissões com autorização deny-by-default na Application. As salvaguardas preservam ao menos uma capacidade efetiva de `roles.manage`. Reset administrativo e troca própria de senha usam a política central de 15–128 caracteres; a troca própria exige a senha atual e usa compare-and-set transacional do hash. Senhas da UI ficam apenas em `PasswordBox`/variáveis transitórias e não entram em ViewModel, sessão, log ou auditoria.

O shell também registra falhas não tratadas do dispatcher WPF, do `AppDomain` e de tasks não observadas. Uma exceção fatal no dispatcher mostra somente mensagem genérica ao usuário, registra detalhes técnicos em log e encerra a aplicação para evitar continuar uma sessão potencialmente inconsistente. Isso complementa, e não substitui, tratamento local de erros recuperáveis.

## Cadastros-base

### Empresas

`Company` contém identidade, nome e status. A tela lista ativas/inativas, cria, renomeia e altera status. Consulta exige `companies.read`; mutações exigem `companies.manage` na Application.

### Filiais

`CompanyBranch` contém identidade, `CompanyId`, nome e status. Cada filial pertence obrigatoriamente a uma empresa por FK `CompanyBranches.CompanyId -> Companies.Id`, com `ON DELETE RESTRICT`. A tela de Empresas possui ação `Gerenciar` para abrir uma janela modal de filiais, que lista ativas/inativas e permite criar, renomear, ativar e desativar.

Filiais reutilizam `companies.read` para consulta e `companies.manage` para mutações. Desativar uma empresa não altera automaticamente o status das filiais. Dados fiscais, endereço, códigos internos, definição de matriz ou outras regras comerciais não foram presumidos.

### Fornecedores

`Supplier` contém identidade, nome e status. O catálogo WPF lista fornecedores ativos/inativos e permite criar, renomear, ativar e desativar. Consulta exige `suppliers.read`; mutações exigem `suppliers.manage` na Application. A migration `20261018142500_AddSuppliers` cria a tabela `Suppliers`.

O modelo de fornecedor ainda não presume CNPJ/CPF, endereço, contatos, condições comerciais, dados bancários ou códigos externos. Esses campos só devem entrar quando o fluxo de negócio exigir regras explícitas.

Próxima frente funcional: **Produtos/materiais**. Unidades/categorias e centros de custo permanecem depois no roadmap. Consulta/retenção operacional da auditoria, smoke WPF em Windows real e benchmark de senha em hardware representativo continuam pendências próprias.

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
