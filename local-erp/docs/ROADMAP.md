# Roadmap

Legenda: `[ ]` pendente, `[-]` em andamento, `[x]` concluído.

## Fase 0 — Pesquisa e arquitetura

- [x] comparar stacks desktop Windows;
- [x] definir .NET 10 LTS + WPF;
- [x] definir SQLite single-PC e PostgreSQL futuro;
- [x] definir arquitetura inicial;
- [x] registrar ADRs iniciais.

## Fase 1 — Fundação técnica

- [x] estruturar solução e projetos;
- [x] configurar `.editorconfig` e análise estática;
- [x] criar shell desktop mínimo;
- [x] separar diretórios de dados do diretório de aplicação;
- [x] configurar logging local;
- [-] executar restore/build/test em Windows com .NET 10 SDK;
- [ ] corrigir qualquer warning/erro real encontrado na primeira compilação;
- [ ] adicionar CI depois que o repositório remoto for definido.

## Fase 2 — Persistência

- [x] modelar primeira entidade técnica/de negócio útil (`Company`);
- [-] criar `DbContext` modelado e migration inicial; modelo concluído, migration aguarda SDK;
- [x] aplicar e validar WAL/foreign keys na inicialização SQLite;
- [x] definir política de `decimal`/valores monetários em ADR;
- [ ] testar migrations e integridade referencial;
- [ ] criar seed de desenvolvimento.

## Fase 3 — Autenticação e autorização

- [ ] selecionar/validar implementação de password hashing;
- [ ] usuários locais;
- [ ] roles/grupos/permissões;
- [ ] autorização na Application;
- [ ] sessão local;
- [ ] testes de segurança e regressão.

## Fase 4 — Shell da aplicação

- [ ] navegação real por módulos;
- [ ] tratamento global de erro;
- [ ] atalhos e foco/teclado;
- [ ] estados loading/empty/error;
- [ ] componentes de tabela/filtro após existir uso real.

## Fase 5 — Cadastros-base

- [ ] empresa/filial;
- [ ] fornecedores;
- [ ] produtos/materiais;
- [ ] unidades/categorias;
- [ ] centros de custo.

## Fase 6 — Primeiro módulo operacional

- [ ] compras: fluxo mínimo a definir com regras de negócio reais.

## Fases posteriores

- [ ] relatórios;
- [ ] administração;
- [ ] auditoria completa;
- [ ] backup/restauração;
- [ ] atualização com migration bundle e recuperação segura;
- [ ] instalador;
- [ ] performance/QA;
- [ ] preparação multiusuário;
- [ ] release 1.0.

## Security / RBAC status — 2026-10-16

- [x] Password hashing abstraction and implementation.
- [x] Local users with normalized unique usernames.
- [x] Authentication service and in-memory user session.
- [x] Roles and permissions.
- [x] User-role and role-permission persistence.
- [x] Deny-by-default authorization service.
- [x] `AddRbacAuthorization` migration validated.
- [ ] Secure first-administrator bootstrap.
- [ ] Login lockout / throttling policy.
- [ ] Security audit trail.

