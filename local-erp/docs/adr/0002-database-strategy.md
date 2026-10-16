# ADR-0002: Estratégia de banco de dados

## Status

Aceito — 2026-10-15

## Contexto

Primeira versão em um computador, com possibilidade futura de vários clientes na rede local.

## Opções consideradas

1. SQLite local;
2. PostgreSQL desde o primeiro dia;
3. SQL Server Express/LocalDB;
4. SQLite em compartilhamento de rede.

## Decisão

- single-PC: **SQLite + EF Core 10**;
- multi-PC futuro: **PostgreSQL cliente/servidor**;
- SQLite em compartilhamento de rede é proibido.

## Motivos

SQLite reduz drasticamente instalação e administração no cenário inicial. PostgreSQL é mais apropriado quando há concorrência real de múltiplos clientes.

## Consequências

- provider-specific code permanece em Persistence;
- migrations de SQLite não serão assumidas como portáveis para PostgreSQL;
- será necessária ferramenta de migração de dados quando o modo multiusuário for implementado;
- valores monetários exigirão decisão explícita devido às limitações do provider SQLite com `decimal`.
