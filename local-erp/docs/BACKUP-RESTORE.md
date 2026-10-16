# Backup e restauração

## Objetivo

Backup só é considerado funcional quando a restauração foi validada.

## Plano para SQLite

- usar API de backup do SQLite/Microsoft.Data.Sqlite;
- nomear arquivos com timestamp UTC e versão do schema;
- manter manifesto com versão da aplicação e hash do arquivo;
- validar abertura e `PRAGMA integrity_check` no backup;
- compactar somente após validação;
- política de retenção configurável;
- impedir restauração sobre banco ativo sem confirmação e backup de segurança.

## Restauração

Fluxo futuro:

1. validar arquivo/manifesto;
2. verificar compatibilidade de schema;
3. criar backup do estado atual;
4. fechar conexões;
5. restaurar para arquivo temporário;
6. verificar integridade;
7. troca atômica quando possível;
8. abrir sistema e executar health check;
9. registrar auditoria.
