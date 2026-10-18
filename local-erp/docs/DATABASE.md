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

`CompanyBranch` contém:

- `Id` — `Guid`, chave primária;
- `CompanyId` — `Guid`, obrigatório;
- `Name` — obrigatório, máximo 200 caracteres;
- `IsActive` — obrigatório.

A migration `20261018105000_AddCompanyBranches` cria `CompanyBranches`, índice `IX_CompanyBranches_CompanyId` e FK `FK_CompanyBranches_Companies_CompanyId`. A FK usa `ON DELETE RESTRICT`: uma filial não pode existir sem empresa e uma futura tentativa de excluir uma empresa referenciada não deve apagar filiais silenciosamente.

Status não possui cascata. Desativar `Company` não altera `CompanyBranch.IsActive`, e vice-versa. Essa independência é deliberada até existir regra comercial explícita.

Dados fiscais, endereço, código de estabelecimento, indicação de matriz e outros atributos não fazem parte do schema atual.

## SQLite: pontos de atenção

- `Foreign Keys=True` é definido explicitamente na connection string;
- `Cache=Shared` não é utilizado;
- WAL é habilitado de forma idempotente antes da janela principal;
- a inicialização valida `journal_mode=wal` e `foreign_keys=1`;
- timestamps técnicos usam UTC em `DateTime`;
- valores monetários seguem ADR-0005: inteiro de 64 bits com escala fixa de 6 casas quando participarem de consultas relacionais;
- nunca usar `double` para valores financeiros apenas para contornar limitação do provider.

## Baseline de migrations — 2026-10-18

Sete migrations validadas no CI:

1. `20261015121025_InitialCreate`;
2. `20261015130329_AddLocalUsers`;
3. `20261015141238_AddRbacAuthorization`;
4. `20261016131952_AddAuthenticationThrottling`;
5. `20261016135247_AddSecurityAuditTrail`;
6. `20261017234500_AddAdministrativeAuditContext`;
7. `20261018105000_AddCompanyBranches`.

O CI executa `has-pending-model-changes` e apply -> rollback 0 -> reapply. O baseline de Filiais está consistente com o snapshot EF.

## Backup

A fase de backup deverá usar `SqliteConnection.BackupDatabase` ou mecanismo equivalente do SQLite, seguida de validação do arquivo gerado.

## Migração para PostgreSQL

A evolução futura deverá conter provider PostgreSQL separado, export/import transacional validado, contagem/checksum lógico, backup obrigatório e estratégia de rollback.
