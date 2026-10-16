# Backlog

## P0 — Próxima execução

- [ ] executar `dotnet restore LocalErp.sln` em ambiente com .NET 10 SDK;
- [ ] executar build Release;
- [ ] executar todos os testes;
- [ ] corrigir todos os erros/warnings reais antes de avançar;
- [ ] executar smoke test do WPF em Windows.

## P1 — Persistência

- [x] fixar `dotnet-ef` como ferramenta local e criar design-time factory segura;
- [x] modelar primeira entidade de negócio (`Company`) e mapping EF Core;
- [ ] gerar primeira migration com `./scripts/new-migration.ps1 InitialCreate` depois que o SDK estiver disponível;
- [x] definir ADR de valores monetários/precisão no SQLite;
- [x] definir pragmas e timeouts SQLite; WAL e foreign keys validados na inicialização;
- [ ] adicionar testes de migration up/down e constraints;
- [ ] seed de desenvolvimento seguro.

## P1 — Segurança

- [ ] comparar bibliotecas Argon2id .NET mantidas antes da Fase 3;
- [ ] definir modelo User/Role/Permission;
- [ ] definir política de lockout e sessão local;
- [ ] projetar trilha de auditoria.

## P2 — Engenharia

- [ ] CI após criação do repositório remoto;
- [ ] após a primeira migration, adicionar `dotnet ef migrations has-pending-model-changes` à validação automatizada;
- [ ] coverage quando a suíte tiver volume suficiente;
- [ ] verificação automatizada de dependências vulneráveis;
- [ ] script de package/release na fase apropriada.

## RBAC milestone closed — 2026-10-16

- [x] Password hashing baseline.
- [x] Local users and authentication/session baseline.
- [x] Roles, permissions and authorization.
- [x] SQLite foreign-key persistence for users/roles/permissions.
- [x] RBAC migration consistency and rollback/reapply validation.

Next security slice:
- [ ] Secure bootstrap of the first administrator.
- [ ] Lockout/throttling and authentication security audit events.

