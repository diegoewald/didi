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
- [x] executar restore/build/test em Windows com .NET 10 SDK;
- [x] fechar baseline sem warnings/erros;
- [x] manter CI Windows permanente.

## Fase 2 — Persistência

- [x] modelar primeira entidade útil (`Company`);
- [x] criar `DbContext` e migrations iniciais;
- [x] aplicar e validar WAL/foreign keys;
- [x] definir política de valores monetários;
- [x] testar migrations e integridade referencial;
- [x] criar seed de desenvolvimento idempotente.

## Fase 3 — Autenticação e autorização

- [x] abstração e implementação de password hashing;
- [x] usuários locais normalizados e únicos;
- [x] autenticação local;
- [x] sessão local em memória;
- [x] roles e permissões;
- [x] persistência user-role e role-permission;
- [x] autorização deny-by-default na Application;
- [x] bootstrap seguro e atômico do primeiro administrador;
- [x] fluxo de startup bootstrap/login/main;
- [x] telas WPF de primeiro acesso e login;
- [x] logout com retorno ao login;
- [x] testes de regressão do baseline de autenticação;
- [x] lockout/throttling persistente por conta;
- [x] hardening de timing para usuário inexistente/inativo/bloqueado;
- [x] trilha de auditoria de autenticação;
- [x] política de senha central aplicada ao bootstrap, criação e troca administrativa;
- [x] ferramenta reproduzível de benchmark de password hashing e baseline Windows CI;
- [ ] benchmark do custo de hash em hardware Windows representativo do ambiente final.

## Fase 4 — Shell da aplicação

- [-] navegação real por módulos;
- [ ] tratamento global de erro além do startup;
- [ ] atalhos e foco/teclado;
- [-] estados loading/empty/error: implementados na administração de usuários, ainda não padronizados para o shell inteiro;
- [ ] componentes de tabela/filtro após existir uso real;
- [ ] smoke visual/interativo do fluxo de autenticação e administração em Windows.

## Fase 5 — Administração de usuários

- [x] Parte A — listagem de usuários ativos e inativos protegida por `users.read`;
- [x] Parte B — criar usuário protegido por `users.manage`, com senha fora do ViewModel;
- [x] Parte C — ativar/desativar usuário protegido por `users.manage`, bloqueando auto-inativação;
- [x] Parte D — reset administrativo de senha protegido por `users.manage`, sem senha no ViewModel;
- [x] Parte E — associar/remover roles, com `roles.read`/`roles.manage` e proteção do último administrador efetivo;
- [x] Parte F — auditar atomicamente as mutações administrativas expostas, distinguindo ator e alvo.

## Fase 6 — Cadastros-base

- [-] empresa/filial: serviço e persistência básicos existem; UI operacional pendente;
- [ ] fornecedores;
- [ ] produtos/materiais;
- [ ] unidades/categorias;
- [ ] centros de custo.

## Fase 7 — Primeiro módulo operacional

- [ ] compras: fluxo mínimo a definir com regras de negócio reais.

## Fases posteriores

- [ ] relatórios;
- [ ] administração completa de roles/permissões, incluindo proteção equivalente ao desativar role/revogar `roles.manage` e auditoria atômica dessas mutações;
- [ ] troca de senha pelo próprio usuário;
- [ ] consulta/retenção operacional da trilha de auditoria;
- [ ] backup/restauração;
- [ ] atualização com migration bundle e recuperação segura;
- [ ] instalador;
- [ ] performance/QA;
- [ ] preparação multiusuário;
- [ ] release 1.0.

## Marco de auditoria e benchmark — 2026-10-16

A autenticação local possui trilha de auditoria persistida em `SecurityAuditEvents`, cobrindo bootstrap, login, falhas, bloqueio/throttling e logout sem armazenar senha, hash, token ou username digitado de conta inexistente. A migration `20261016135247_AddSecurityAuditTrail` está integrada e validada no ciclo apply -> rollback 0 -> reapply.

A ferramenta `LocalErp.PasswordHashBenchmark` também está integrada e foi executada em Windows CI com o serviço real do ERP. O baseline básico de segurança foi revisado sem defeito bloqueante. O benchmark em hardware representativo, o smoke visual WPF, a política de retenção e o comportamento desejado para falhas standalone de auditoria continuam como dívidas explícitas de pré-release/governança.

## Marco de administração de usuários — Parte A — 2026-10-16

A área Administração possui tela WPF real de listagem, incluindo usuários ativos e inativos. O carregamento é feito por `UserService`, que mantém a autorização `users.read` na Application. A UI contém estados de carregamento, erro e lista vazia, sem introduzir regra de autorização no code-behind.

## Marco de administração de usuários — Parte B — 2026-10-16

A mesma área permite criar contas com confirmação de senha. `UserService.CreateAsync` exige `users.manage`, rejeita duplicidade normalizada, valida a política de 15–128 caracteres e só então faz o hashing. `ChangePasswordAsync` usa a mesma política central.

A senha é capturada e limpa em `PasswordBox`, segue diretamente da View para a camada Application e não passa pelo ViewModel. O resultado seguro da criação é o único dado encaminhado ao ViewModel para atualizar a tabela. Nenhuma migration nova foi necessária e o baseline subiu para 87/87 testes.

## Marco de administração de usuários — Parte C — 2026-10-17

A tabela de usuários ganhou ação de ativar/desativar. `UserService.SetActiveAsync` continua exigindo `users.manage` na Application e rejeita a tentativa de desativar a própria conta da sessão antes de qualquer persistência. A View pede confirmação antes da desativação e apresenta feedback de sucesso/erro; reativação permanece disponível para contas inativas.

Não foi criada consulta artificial de “último administrador” neste slice: o próprio operador já precisa estar ativo e autorizado com `users.manage`, e sua auto-inativação é bloqueada. Assim, desativar outra conta não remove o último operador atual. A regra para não remover a última role/capacidade administrativa pertence à Parte E, quando associação e remoção de roles forem expostas.

Nenhuma migration nova foi necessária. O baseline automatizado subiu para 91/91 testes, com quatro novos testes de Application cobrindo autorização, ativação, desativação de outra conta e bloqueio de auto-inativação.

## Marco de administração de usuários — Parte D — 2026-10-17

A tabela administrativa ganhou ação de redefinição de senha. A View abre um painel com dois `PasswordBox`, exige confirmação da nova senha, mantém senha/confirmação fora do ViewModel e limpa os campos após sucesso ou falha. Mudanças de status e reset de senha não podem ser disparadas concorrentemente na tela.

`UserService.ChangePasswordAsync` continua exigindo `users.manage` e `PasswordPolicy` de 15–128 caracteres. Além de persistir o novo hash, o reset administrativo zera `FailedSignInCount` e `SignInBlockedUntilUtc` na mesma atualização, permitindo recuperação de conta bloqueada sem alterar `IsActive`. Usuário inexistente é rejeitado antes do hashing.

Nenhuma migration nova foi necessária. O baseline automatizado subiu para 94/94 testes (Application 45/45), com cobertura explícita da permissão `users.manage`, limpeza do estado de lockout preservando conta inativa e ausência de hashing quando o usuário não existe.

## Marco de administração de usuários — Parte E — 2026-10-17

A tabela administrativa ganhou ação `Gerenciar` para roles. A janela modal lista roles ativas/inativas e a associação do usuário. `RoleService.ListUserAssignmentsAsync` exige `roles.read`; `AssignUserAsync` e `RemoveUserAsync` exigem `roles.manage`. Usuários inativos e roles inativas não recebem novas associações, mas vínculos existentes continuam removíveis.

A remoção administrativa protege a capacidade raiz `roles.manage`. A persistência executa em transação SQLite imediata: se o vínculo alvo participa de um caminho efetivo de administração, a exclusão só ocorre quando existe outro caminho formado por usuário ativo + role ativa + grant `roles.manage`. Assim, a regra é persistente e resistente a duas remoções concorrentes, não apenas uma validação de UI.

Self-demotion é permitida quando outro administrador efetivo permanece; se não permanecer, a Application lança `LastAdministrativeRoleAssignmentException`. A futura administração completa de roles deverá aplicar proteção equivalente antes de permitir desativar uma role ou revogar seu `roles.manage`, pois essas mutações não são expostas neste slice.

Nenhuma migration nova foi necessária. O baseline automatizado subiu para 101/101 testes (Domain 19, Application 49, Persistence 27, Infrastructure 6), incluindo teste concorrente que confirma exatamente uma remoção e um bloqueio quando dois últimos administradores são removidos simultaneamente.

## Marco de administração de usuários — Parte F — 2026-10-17

A auditoria administrativa passou a distinguir de forma estruturada o usuário afetado (`UserId`), o operador autenticado (`ActorUserId`) e a role afetada quando aplicável (`RoleId`). Os eventos usam códigos canônicos para criação de usuário, ativação, desativação, reset administrativo, associação e remoção de role, sem senha, hash, token ou payload textual arbitrário.

`IAdministrativeAuditStore` concentra a persistência atômica. Criação/status/reset de usuário e o evento correspondente compartilham o mesmo `SaveChanges`; associação/remoção de role e seu evento compartilham uma transação SQLite imediata. Testes de persistência forçam falha de FK no ator e confirmam rollback da mutação, enquanto o teste concorrente de remoção confirma que apenas a alteração efetivamente commitada produz evento.

A migration `20261017234500_AddAdministrativeAuditContext` adiciona contexto de ator/role com FKs opcionais `SET NULL` e índices por ator/data e role/data. Como SQLite não suporta `AddForeignKey` sobre tabela já criada, a versão final da migration reconstrói `SecurityAuditEvents`, copia o histórico existente e recria FKs/índices; o CI validou apply -> rollback 0 -> reapply.

O baseline automatizado subiu para 110/110 testes (Domain 19, Application 54, Persistence 31, Infrastructure 6), com build Release e benchmark sem warnings/erros e snapshot EF sem mudanças pendentes.

Próxima prioridade funcional: administração completa de roles/permissões. Antes de expor desativação de role ou revogação de `roles.manage`, aplicar salvaguarda equivalente à Parte E e integrar essas futuras mutações à auditoria atômica.
