# Segurança

## Princípios

- zero senha em texto puro;
- zero segredo versionado;
- autorização na camada de aplicação;
- consultas parametrizadas/EF Core;
- validação de entrada em fronteiras;
- logs sem credenciais ou conteúdo sensível;
- princípio do menor privilégio;
- ações administrativas auditáveis.

## Password hashing

Preferência: Argon2id conforme recomendação atual da OWASP, após validação de biblioteca .NET mantida, licença e parâmetros.

Fallback: `PasswordHasher<TUser>` da Microsoft. Não criar algoritmo próprio.

A escolha final será registrada em ADR no início da Fase 3.

## Segredos locais

Quando houver necessidade de segredo local (por exemplo, pepper ou credencial de integração), avaliar Windows DPAPI/Credential Manager. Segredos não devem ficar em `appsettings.json` versionado.

## Permissões

O modelo futuro deve suportar permissões por operação, por exemplo:

```text
suppliers.read
suppliers.create
suppliers.update
suppliers.delete
purchase_orders.approve
reports.export
admin.users.manage
```

A UI pode ocultar/desabilitar comandos, mas a Application deve negar a operação independentemente da UI.

## Auditoria

Registrar ações críticas sem armazenar senha, token ou payload sensível integral.

## Dependências

Antes de atualizar/adicionar dependências:

- verificar release estável;
- vulnerabilidades conhecidas;
- manutenção;
- licença;
- breaking changes.

## RBAC authorization baseline (2026-10-16)

- permissions are assigned to roles;
- users receive one or more roles;
- protected application operations deny access by default unless the current user has the required permission;
- normalized role names are unique;
- user-role and role-permission associations are protected by relational foreign keys;
- password hashes are not exposed in application result DTOs.

Secure first-administrator bootstrap, lockout policy and security audit trail remain follow-up work and must not be bypassed by special-case authorization shortcuts.

