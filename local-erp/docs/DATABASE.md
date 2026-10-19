# Banco de dados

## Estratégia

### Single-PC

SQLite local em `%LocalAppData%\LocalErp\Data\localerp.db`.

### Multi-PC futuro

PostgreSQL em servidor da rede local. O arquivo SQLite **não** será colocado em compartilhamento SMB para acesso concorrente por vários computadores.

## ORM e migrations

Entity Framework Core 10 com migrations versionadas.

Regras:

- nenhuma alteração manual silenciosa de schema em produção;
- toda migration deve ser revisada;
- migrations destrutivas exigem estratégia de dados/rollback;
- foreign keys e constraints são preferidas a validação apenas na UI;
- índices surgem por necessidade de consulta medida/documentada.

`dotnet-ef` é fixado no manifesto `.config/dotnet-tools.json`. A `ErpDbContextDesignTimeFactory` usa banco temporário em `%TEMP%/LocalErp/DesignTime`, evitando apontar tooling para dados reais.

## Empresas

`Company` contém identidade, nome (`max 200`) e `IsActive`. O `Guid` é criado pelo domínio e o mapping usa `ValueGeneratedNever`.

## Filiais

`CompanyBranch` contém `Id`, `CompanyId`, `Name` e `IsActive`. A migration `20261018105000_AddCompanyBranches` cria `CompanyBranches`, índice `IX_CompanyBranches_CompanyId` e FK `FK_CompanyBranches_Companies_CompanyId` com `ON DELETE RESTRICT`.

Status não possui cascata. Desativar `Company` não altera `CompanyBranch.IsActive`, e vice-versa. Dados fiscais, endereço, código de estabelecimento, indicação de matriz e outros atributos não fazem parte do schema atual.

## Fornecedores

`Supplier` contém:

- `Id` — `Guid`, chave primária criada pelo domínio;
- `Name` — obrigatório, normalizado e limitado a 200 caracteres;
- `IsActive` — obrigatório.

A migration `20261018142500_AddSuppliers` cria a tabela `Suppliers`. Nesta etapa não há CNPJ/CPF, endereço, contatos, dados bancários, condição de pagamento ou códigos externos. Esses campos dependem de requisitos explícitos do fluxo de compras e não devem ser inferidos antecipadamente.

## Materiais

`Material` contém:

- `Id` — `Guid`, chave primária criada pelo domínio;
- `Name` — obrigatório, normalizado e limitado a 200 caracteres;
- `IsActive` — obrigatório.

A migration `20261018222000_AddMaterials` cria a tabela `Materials`. Material e Produto permanecem conceitos distintos. Código/SKU, unidade, categoria, estoque, preço, fornecedor padrão e demais vínculos comerciais não fazem parte do schema atual e só devem ser adicionados quando houver requisito explícito.

## Produtos

`Product` contém:

- `Id` — `Guid`, chave primária criada pelo domínio;
- `Name` — obrigatório, normalizado e limitado a 200 caracteres;
- `IsActive` — obrigatório.

A migration `20261019005000_AddProducts` cria a tabela `Products`. Produto não possui FK ou associação implícita com `Material`. Código/SKU, unidade, categoria, estoque, preço, tributação, composição/BOM e vínculos comerciais permanecem fora do schema até existir requisito explícito.

## Unidades de medida

`MeasurementUnit` contém:

- `Id` — `Guid`, chave primária criada pelo domínio;
- `Name` — obrigatório, normalizado e limitado a 200 caracteres;
- `IsActive` — obrigatório.

A migration `20261019093000_AddMeasurementUnits` cria a tabela `MeasurementUnits`. O schema não possui sigla/símbolo, precisão decimal, fatores de conversão ou FKs para `Product`/`Material`. Esses conceitos só devem ser introduzidos quando suas regras de negócio forem explicitamente definidas.

## SQLite: pontos de atenção

- `Foreign Keys=True` é definido explicitamente na connection string;
- `Cache=Shared` não é utilizado;
- WAL é habilitado de forma idempotente antes da janela principal;
- a inicialização valida `journal_mode=wal` e `foreign_keys=1`;
- timestamps técnicos usam UTC em `DateTime`;
- valores monetários seguem ADR-0005: inteiro de 64 bits com escala fixa de 6 casas quando participarem de consultas relacionais;
- nunca usar `double` para valores financeiros apenas para contornar limitação do provider.

## Baseline de migrations — 2026-10-19

Onze migrations validadas no CI:

1. `20261015121025_InitialCreate`;
2. `20261015130329_AddLocalUsers`;
3. `20261015141238_AddRbacAuthorization`;
4. `20261016131952_AddAuthenticationThrottling`;
5. `20261016135247_AddSecurityAuditTrail`;
6. `20261017234500_AddAdministrativeAuditContext`;
7. `20261018105000_AddCompanyBranches`;
8. `20261018142500_AddSuppliers`;
9. `20261018222000_AddMaterials`;
10. `20261019005000_AddProducts`;
11. `20261019093000_AddMeasurementUnits`.

O CI executa `has-pending-model-changes` e apply -> rollback 0 -> reapply. O aviso conhecido do rollback de `AddAuthenticationThrottling` sobre `PRAGMA foreign_keys = 0` permanece não bloqueante e deve ser revisto antes de release sem alterar histórico de migration de forma insegura.

## Backup

A fase de backup deverá usar `SqliteConnection.BackupDatabase` ou mecanismo equivalente do SQLite, seguida de validação do arquivo gerado.

## Migração para PostgreSQL

A evolução futura deverá conter provider PostgreSQL separado, export/import transacional validado, contagem/checksum lógico, backup obrigatório e estratégia de rollback.
