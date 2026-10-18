# Backlog

## P0 — Pré-release / validação de ambiente

- [ ] executar smoke visual/interativo do WPF em Windows real;
- [ ] executar benchmark do password hashing em hardware Windows representativo do ambiente alvo.

## P1 — Segurança e administração

- [x] autenticação local, sessão, RBAC e autorização deny-by-default;
- [x] bootstrap seguro do primeiro administrador;
- [x] throttling/lockout e mitigação de timing;
- [x] trilha persistida de auditoria de autenticação;
- [x] administração de usuários Partes A–F;
- [x] administração completa de roles/permissões;
- [x] salvaguardas da última capacidade efetiva de `roles.manage`;
- [x] auditoria atômica das mutações administrativas expostas;
- [x] troca de senha pelo próprio usuário com senha atual, compare-and-set e auditoria atômica;
- [ ] tela/consulta administrativa da trilha de auditoria quando houver necessidade operacional;
- [ ] definir política de retenção/limpeza de `SecurityAuditEvents`;
- [ ] avaliar política para falhas de escrita de auditoria em caminhos de login que não alteram estado.

## P1 — Cadastros-base

- [x] empresa: domínio/persistência existentes e UI operacional em `Cadastros`;
- [x] empresa: listagem de ativos/inativos, criação, renomeação e status;
- [x] empresa: `companies.read` para consulta e `companies.manage` para mutações;
- [x] filial: entidade `CompanyBranch` com `CompanyId`, nome e status;
- [x] filial: FK obrigatória para `Company` com `ON DELETE RESTRICT` e índice por `CompanyId`;
- [x] filial: serviço/repositório com listagem por empresa, criação, renomeação e status;
- [x] filial: reutilizar `companies.read/manage` sem criar permissão redundante;
- [x] filial: janela WPF acessível pelo botão `Gerenciar` de cada empresa;
- [x] filial: listar ativas/inativas, criar, renomear e ativar/desativar;
- [x] migration `20261018105000_AddCompanyBranches` validada em apply/rollback/reapply;
- [ ] manter dados fiscais, endereço, códigos internos e regra de matriz fora do modelo até requisito explícito;
- [ ] fornecedores;
- [ ] produtos/materiais;
- [ ] unidades/categorias;
- [ ] centros de custo.

## P1 — Persistência

- [x] tooling local do `dotnet-ef`, WAL e foreign keys;
- [x] migrations iniciais e ciclo up/down no CI;
- [x] RBAC e auditoria com FKs reais;
- [x] transações SQLite imediatas nos fluxos concorrentes críticos;
- [x] `CompanyBranches` com integridade referencial para `Companies`;
- [x] teste de persistência rejeitando filial órfã por FK.

## P2 — Engenharia

- [x] CI Windows permanente;
- [x] `has-pending-model-changes` no CI;
- [x] build da ferramenta de benchmark no CI;
- [ ] coverage quando a suíte tiver volume suficiente;
- [ ] verificação automatizada de dependências vulneráveis;
- [ ] script de package/release;
- [ ] teste automatizado de UI somente para fluxos cujo custo se justificar.

## Baseline atual — 2026-10-18

Empresa e filial estão operacionais no cadastro-base. `CompanyBranchService` valida a existência da empresa antes da criação/listagem, mantém autorização em `companies.read/manage` e não introduz regra implícita de cascata de status. A persistência usa FK obrigatória com `RESTRICT`; filial órfã é rejeitada pelo SQLite.

Baseline automatizado: 138/138 testes (Domain 22, Application 69, Persistence 41, Infrastructure 6), 7 migrations, build Release e benchmark tool sem warnings/erros, snapshot EF sem mudanças pendentes e apply -> rollback 0 -> reapply validado.

Próxima tarefa funcional: **Fornecedores**. Não antecipar dados fiscais/endereço de filial nem regras de matriz sem requisito de negócio. Smoke WPF e benchmark em hardware representativo permanecem pendências de pré-release.
