# ADR-0006: Aplicação de migrations em produção

## Status

Aceito.

## Contexto

O ERP começa como aplicação Windows local com SQLite, mas precisa preservar dados de forma segura durante atualizações. O EF Core permite aplicar migrations diretamente pela aplicação com `Database.MigrateAsync()`, porém a documentação oficial recomenda cautela em produção porque o aplicativo passa a modificar o schema diretamente e os comandos deixam de passar por uma etapa explícita de revisão operacional.

Também não é aceitável combinar `EnsureCreated` com migrations: `EnsureCreated` ignora o histórico de migrations e inviabiliza a evolução normal do schema.

## Opções consideradas

1. Executar `Database.MigrateAsync()` toda vez que o ERP iniciar.
2. Gerar e executar um migration bundle versionado durante instalação/atualização.
3. Gerar scripts SQL e exigir aplicação manual pelo usuário/administrador.

## Decisão

Para produção, migrations serão:

1. geradas pelas ferramentas do EF Core;
2. revisadas em código;
3. exercitadas em testes de integração;
4. empacotadas em um migration bundle compatível com a release;
5. executadas pelo fluxo de instalação/atualização, antes de abrir a nova versão do ERP;
6. precedidas por backup validado sempre que houver alteração de schema em uma base existente;
7. seguidas por um health check mínimo do banco.

O aplicativo principal **não** executará `EnsureCreated`, `Migrate` ou `MigrateAsync` automaticamente em seu startup normal de produção.

Em desenvolvimento e testes, a CLI do EF Core poderá aplicar migrations em bancos descartáveis.

O rollback operacional de uma atualização com alteração de schema será baseado prioritariamente na restauração do backup pré-update e da versão anterior da aplicação, em vez de depender de uma migration `Down` destrutiva em dados reais.

## Consequências

### Positivas

- schema de produção só muda dentro de um processo explícito de atualização;
- migrations podem ser revisadas e testadas antes de tocar dados reais;
- reduz o risco de uma inicialização comum alterar schema inesperadamente;
- encaixa com backup pré-update e recuperação da versão anterior;
- mantém a aplicação principal com menos responsabilidades de implantação.

### Custos

- a pipeline de release precisará gerar e testar migration bundles;
- o atualizador terá de orquestrar backup, bundle, validação e rollback;
- primeira instalação também precisará executar o bundle inicial antes do primeiro uso.

## Regras adicionais

- CI deverá verificar alterações pendentes no modelo com `dotnet ef migrations has-pending-model-changes` quando a primeira migration existir;
- migration gerada nunca é aceita sem revisão do diff;
- migrations destrutivas exigem plano explícito de preservação/transformação de dados;
- o banco real do usuário nunca será usado por testes automatizados.
