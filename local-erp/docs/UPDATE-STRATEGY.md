# Estratégia de atualização

## Ferramenta prevista

Velopack, implementado somente quando o núcleo estiver estável.

## Fluxo obrigatório

1. consultar versão disponível;
2. validar canal e compatibilidade;
3. baixar pacote;
4. validar integridade/assinatura;
5. gerar backup pré-update quando houver alteração de schema;
6. aplicar binários;
7. executar o migration bundle versionado da release quando houver mudança de schema;
8. validar o schema e a integridade mínima do banco;
9. iniciar nova versão;
10. executar health check mínimo;
11. registrar sucesso/falha.

## Regras

- update nunca sobrescreve diretórios de dados do usuário;
- migrations destrutivas não entram em atualização automática sem plano de recuperação;
- versão anterior deve permanecer recuperável quando tecnicamente viável;
- atualização online é opcional: o ERP continua operando offline na versão instalada.


## Aplicação de schema

A aplicação desktop não executará `EnsureCreated`, `Migrate` ou `MigrateAsync` automaticamente em produção. Migrations revisadas serão empacotadas em migration bundle e executadas pelo fluxo de instalação/atualização, conforme ADR-0006.
