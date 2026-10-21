# Local ERP

ERP desktop local-first para Windows, desenvolvido como produto de longo prazo.

## Estado

- Versão: `0.1.0`
- Fase: segurança/administração e cadastros-base fechados; Fase 7 de Compras em andamento com Solicitação de Compra e Cotação/RFQ operacionais
- UI: WPF em .NET 10 LTS
- Persistência: SQLite via EF Core 10
- Estratégia futura de rede: PostgreSQL em arquitetura cliente/servidor
- CI Windows: build Release com 0 warnings / 0 errors e 235/235 testes passando
- Migrations: 15 migrations, snapshot consistente e ciclo apply -> rollback 0 -> reapply validado

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

### Materiais

`Material` é um conceito separado de Produto e contém, nesta etapa, apenas identidade, nome e status. `MaterialService` exige `materials.read` para consulta e `materials.manage` para criação, renomeação e status. `EfMaterialRepository` persiste o cadastro em `Materials`, criado pela migration `20261018222000_AddMaterials`.

O catálogo WPF de Materiais está disponível em `Cadastros`, com listagem de ativos/inativos, estados loading/erro/vazio, criação, renomeação e ativação/desativação. A UI apenas apresenta e orquestra interação; autorização e regras continuam na Application.

Código/SKU, unidade, categoria, estoque, preço e vínculos comerciais não foram presumidos. Instalações novas incluem `materials.read/manage` no bootstrap do primeiro administrador; roles já existentes mantêm grants explícitos e precisam receber novas permissões deliberadamente por um operador com `roles.manage`.

### Produtos

`Product` é um cadastro próprio e permanece separado de `Material`. O modelo contém somente identidade, nome normalizado de até 200 caracteres e status ativo/inativo. `ProductService` exige `products.read` para consulta e `products.manage` para criação, renomeação e status. `EfProductRepository` persiste o cadastro em `Products`, criado pela migration `20261019005000_AddProducts`.

O catálogo WPF de Produtos está disponível em `Cadastros`, com estados loading/erro/vazio, listagem de ativos/inativos, criação, renomeação via Enter/Escape e ativação/desativação com confirmação. A UI não replica regras de autorização: todas as demandas continuam na Application.

Código/SKU, unidade, categoria, estoque, preço, tributação, composição/BOM e vínculos comerciais não foram presumidos. Instalações novas recebem `products.read/manage` pelo catálogo de permissões do bootstrap; roles existentes não recebem elevação automática e devem receber grants explicitamente quando necessário.

### Unidades de medida

`MeasurementUnit` é um cadastro global independente e contém somente identidade, nome normalizado de até 200 caracteres e status ativo/inativo. `MeasurementUnitService` exige `units.read` para consulta e `units.manage` para criação, renomeação e status. `EfMeasurementUnitRepository` persiste o cadastro em `MeasurementUnits`, criado pela migration `20261019093000_AddMeasurementUnits`.

O catálogo WPF de Unidades de Medida está disponível em `Cadastros`, com estados loading/erro/vazio, listagem de ativos/inativos, criação, renomeação via Enter/Escape e ativação/desativação com confirmação. A UI apenas orquestra a interação e mantém autorização e regras na Application.

Sigla/símbolo, precisão, fatores de conversão e vínculos automáticos com Produto ou Material não foram presumidos. Instalações novas recebem `units.read/manage` no bootstrap; roles existentes não recebem grants novos automaticamente.

### Categorias

`Category` é um cadastro global independente e contém somente identidade, nome normalizado de até 200 caracteres e status ativo/inativo. `CategoryService` exige `categories.read` para consulta e `categories.manage` para criação, renomeação e status. `EfCategoryRepository` persiste o cadastro em `Categories`, criado pela migration `20261020113000_AddCategories`.

O catálogo WPF de Categorias está disponível em `Cadastros`, com estados loading/erro/vazio, listagem de ativos/inativos, criação, renomeação via Enter/Escape e ativação/desativação com confirmação. A UI apenas orquestra a interação; autorização e regras permanecem na Application.

Não existe hierarquia, categoria-pai, código ou vínculo automático com Produto ou Material. Instalações novas recebem `categories.read/manage` no bootstrap; roles existentes não recebem grants novos automaticamente.

### Centros de custo

`CostCenter` é um cadastro global independente e contém somente identidade, nome normalizado de até 200 caracteres e status ativo/inativo. `CostCenterService` exige `cost-centers.read` para consulta e `cost-centers.manage` para criação, renomeação e status. `EfCostCenterRepository` persiste o cadastro em `CostCenters`, criado pela migration `20261021100000_AddCostCenters`.

O catálogo WPF de Centros de Custo está disponível em `Cadastros`, com estados loading/erro/vazio, listagem de ativos/inativos, criação, renomeação via Enter/Escape e ativação/desativação com confirmação. A UI apenas orquestra interação; autorização e regras permanecem na Application.

Código, hierarquia, vínculo obrigatório com empresa/filial, regra financeira e relacionamentos automáticos não foram presumidos. Instalações novas recebem `cost-centers.read/manage` no bootstrap; roles existentes não recebem grants novos automaticamente.

Empresa, filial, fornecedor, material, produto, unidade de medida, categoria e centro de custo estão operacionais na UI. A Fase 6 de cadastros-base está concluída.

O módulo Compras possui dois slices verticais operacionais: **Solicitação de Compra** e **Cotação/RFQ**. O RFQ nasce de uma Solicitação `Submitted`, seleciona linhas de origem, convida fornecedores, registra e finaliza respostas com preço unitário/data prometida e permite encerramento ou cancelamento. Ele não escolhe vencedor automaticamente e não cria Pedido de Compra.

A próxima frente funcional é **especificar o terceiro slice de Compras antes de nova implementação**, mantendo aprovação, seleção de vencedor e Pedido de Compra fora do código até suas regras serem documentadas. Consulta/retenção operacional da auditoria, smoke WPF em Windows real e benchmark de senha em hardware representativo continuam pendências próprias.

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
