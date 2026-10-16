# Banco de dados

## Estratégia

### Single-PC

SQLite local em `%LocalAppData%\LocalErp\Data\localerp.db`.

### Multi-PC futuro

PostgreSQL em servidor da rede local. O arquivo SQLite **não** será colocado em compartilhamento SMB para acesso concorrente por vários computadores.

## ORM

Entity Framework Core 10 com migrations versionadas.

Regras:

- nenhuma alteração manual silenciosa de schema em produção;
- toda migration deve ser revisada;
- migrations destrutivas exigem estratégia de dados/rollback;
- foreign keys e constraints são preferidas a validação apenas na UI;
- índices surgem por necessidade de consulta medida/documentada.

## Primeiro modelo persistido

`Company` é a primeira entidade de negócio modelada. Nesta fase ela contém apenas identidade, nome e estado ativo/inativo; dados fiscais e demais atributos serão adicionados somente quando houver requisitos explícitos, evitando antecipar regras de negócio.

O mapping EF Core é separado da entidade de domínio e usa `ValueGeneratedNever` para o `Guid` criado pelo domínio.

## SQLite: pontos de atenção

O provider EF Core SQLite possui limitações em operações de migration e em tipos como `decimal`, `DateTimeOffset` e `TimeSpan` para ordenação/comparação.

Por isso:

- timestamps técnicos serão UTC em `DateTime`;
- valores monetários persistidos no SQLite seguirão o ADR-0005: inteiro de 64 bits com escala fixa de 6 casas quando precisarem participar de consultas relacionais;
- nunca usar `double` para valores financeiros apenas para contornar limitação do provider;
- arredondamento comercial permanece regra explícita de cada fluxo e deverá possuir testes próprios.

## Conexões e concorrência local

- `Foreign Keys=True` é definido explicitamente na connection string;
- `Cache=Shared` não é utilizado, pois o próprio SQLite desencoraja shared-cache e a documentação do Microsoft.Data.Sqlite recomenda removê-lo quando WAL é utilizado;
- o timeout padrão é explicitado na connection string;
- WAL é habilitado de forma idempotente pelo `SqliteDatabaseRuntimeConfigurator` antes da abertura da janela principal;
- a inicialização valida que `journal_mode=wal` e `foreign_keys=1`, falhando cedo se a configuração não puder ser aplicada;
- migrations continuam separadas dessa configuração de runtime e só serão geradas pelas ferramentas do EF Core.

## Backup

A fase de backup deverá usar a API `SqliteConnection.BackupDatabase` ou mecanismo equivalente do SQLite, seguida de validação do arquivo gerado.

## Migração para PostgreSQL

A evolução futura deverá conter:

1. provider PostgreSQL separado;
2. migrations específicas quando necessário;
3. export/import transacional e validado;
4. contagem e checksum lógico dos registros migrados;
5. backup obrigatório antes da conversão;
6. ferramenta de diagnóstico e rollback da migração.


## Tooling de migrations

`dotnet-ef` é fixado como ferramenta local no manifesto `.config/dotnet-tools.json`, na mesma versão do EF Core usado pela solução.

A `ErpDbContextDesignTimeFactory` usa um banco temporário em `%TEMP%/LocalErp/DesignTime` apenas para design-time, evitando que a geração de migration dependa do WPF ou aponte acidentalmente para dados reais.

Com SDK disponível:

```powershell
./scripts/new-migration.ps1 InitialCreate
```

A migration gerada deve ser revisada antes de ser commitada/aplicada.
