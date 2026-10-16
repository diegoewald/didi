# ADR-0004: Arquitetura em camadas

## Status

Aceito — 2026-10-15

## Contexto

O ERP terá muitos módulos, regras, permissões, relatórios e persistência; a UI não pode se tornar o centro da lógica.

## Decisão

Separar em Domain, Application, Infrastructure, Persistence e Desktop, introduzindo abstrações apenas quando houver uso real.

## Motivos

- protege regras de negócio da tecnologia de UI/banco;
- facilita testes;
- reduz impacto da futura mudança SQLite → PostgreSQL;
- prepara autorização e auditoria fora da interface;
- evita uma Clean Architecture excessivamente cerimonial.

## Consequências

- composição de dependências fica no Desktop;
- regras de referência entre projetos devem ser respeitadas;
- não criar repositórios genéricos ou CQRS sem problema concreto que os justifique.
