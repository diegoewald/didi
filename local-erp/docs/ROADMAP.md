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
- [x] política de senha central aplicada ao bootstrap, criação, reset administrativo e troca própria;
- [x] ferramenta reproduzível de benchmark de password hashing e baseline Windows CI;
- [ ] benchmark do custo de hash em hardware Windows representativo do ambiente final.

## Fase 4 — Shell da aplicação

- [-] navegação real por módulos;
- [ ] tratamento global de erro além do startup;
- [ ] atalhos e foco/teclado;
- [-] estados loading/empty/error: administração e empresas possuem estados próprios, ainda não padronizados para o shell inteiro;
- [ ] componentes de tabela/filtro após existir uso real;
- [ ] smoke visual/interativo do fluxo de autenticação, administração e cadastros em Windows.

## Fase 5 — Administração de usuários e roles

- [x] Parte A — listagem de usuários ativos e inativos protegida por `users.read`;
- [x] Parte B — criar usuário protegido por `users.manage`, com senha fora do ViewModel;
- [x] Parte C — ativar/desativar usuário protegido por `users.manage`, bloqueando auto-inativação;
- [x] Parte D — reset administrativo de senha protegido por `users.manage`, sem senha no ViewModel;
- [x] Parte E — associar/remover roles, com `roles.read`/`roles.manage` e proteção do último administrador efetivo;
- [x] Parte F — auditar atomicamente as mutações administrativas expostas, distinguindo ator e alvo;
- [x] administração completa de roles/permissões com criação, status e grants/revogações;
- [x] proteger desativação de role e revogação de `roles.manage` contra perda da última capacidade efetiva;
- [x] auditar atomicamente criação/status de role e grant/revogação de permissões;
- [x] troca de senha pelo próprio usuário com validação da senha atual e auditoria atômica.

## Fase 6 — Cadastros-base

- [x] empresa (`Company`): UI operacional em `Cadastros` com listagem, criação, renomeação e status;
- [-] filial: modelo e vínculo com empresa ainda pendentes; não inventar campos fiscais/endereço antes do requisito;
- [ ] fornecedores;
- [ ] produtos/materiais;
- [ ] unidades/categorias;
- [ ] centros de custo.

## Fase 7 — Primeiro módulo operacional

- [ ] compras: fluxo mínimo a definir com regras de negócio reais.

## Fases posteriores

- [ ] relatórios;
- [ ] consulta/retenção operacional da trilha de auditoria;
- [ ] backup/restauração;
- [ ] atualização com migration bundle e recuperação segura;
- [ ] instalador;
- [ ] performance/QA;
- [ ] preparação multiusuário;
- [ ] release 1.0.

## Marco de auditoria e benchmark — 2026-10-16

A autenticação local possui trilha de auditoria persistida em `SecurityAuditEvents`, cobrindo bootstrap, login, falhas, bloqueio/throttling e logout sem armazenar senha, hash, token ou username digitado de conta inexistente. A migration `20261016135247_AddSecurityAuditTrail` está integrada e validada no ciclo apply -> rollback 0 -> reapply.

A ferramenta `LocalErp.PasswordHashBenchmark` também está integrada e foi executada em Windows CI com o serviço real do ERP. O benchmark em hardware representativo, o smoke visual WPF, a política de retenção e o comportamento desejado para falhas standalone de auditoria continuam como dívidas explícitas de pré-release/governança.

## Marco de administração de usuários — Parte A — 2026-10-16

A área Administração possui tela WPF real de listagem, incluindo usuários ativos e inativos. O carregamento é feito por `UserService`, que mantém a autorização `users.read` na Application. A UI contém estados de carregamento, erro e lista vazia, sem introduzir regra de autorização no code-behind.

## Marco de administração de usuários — Parte B — 2026-10-16

A mesma área permite criar contas com confirmação de senha. `UserService.CreateAsync` exige `users.manage`, rejeita duplicidade normalizada, valida a política de 15–128 caracteres e só então faz o hashing. A senha é capturada e limpa em `PasswordBox`, segue diretamente da View para a camada Application e não passa pelo ViewModel. O baseline subiu para 87/87 testes.

## Marco de administração de usuários — Parte C — 2026-10-17

A tabela de usuários ganhou ação de ativar/desativar. `UserService.SetActiveAsync` continua exigindo `users.manage` na Application e rejeita a tentativa de desativar a própria conta da sessão antes de qualquer persistência. A View pede confirmação antes da desativação e permite reativação. O baseline subiu para 91/91 testes.

## Marco de administração de usuários — Parte D — 2026-10-17

A tabela administrativa ganhou ação de redefinição de senha. `UserService.ChangePasswordAsync` exige `users.manage`, aplica `PasswordPolicy`, zera `FailedSignInCount`/`SignInBlockedUntilUtc`, preserva `IsActive` e mantém senha/confirmação fora do ViewModel. O baseline subiu para 94/94 testes.

## Marco de administração de usuários — Parte E — 2026-10-17

A tabela administrativa ganhou ação `Gerenciar` para roles. `RoleService.ListUserAssignmentsAsync` exige `roles.read`; `AssignUserAsync` e `RemoveUserAsync` exigem `roles.manage`. Usuários inativos e roles inativas não recebem novas associações, mas vínculos existentes continuam removíveis.

A remoção administrativa protege a capacidade raiz `roles.manage` em transação SQLite imediata. Se o vínculo alvo participa do último caminho efetivo de administração, a operação é rejeitada. O teste concorrente confirma que duas remoções simultâneas dos dois últimos administradores deixam exatamente um administrador efetivo. O baseline subiu para 101/101 testes.

## Marco de administração de usuários — Parte F — 2026-10-17

A auditoria administrativa passou a distinguir de forma estruturada o usuário afetado (`UserId`), o operador autenticado (`ActorUserId`) e a role afetada quando aplicável (`RoleId`). Os eventos usam códigos canônicos sem senha, hash, token ou payload textual arbitrário.

`IAdministrativeAuditStore` concentra a persistência atômica. Criação/status/reset de usuário e associação/remoção de role compartilham a unidade de persistência com seu evento. A migration `20261017234500_AddAdministrativeAuditContext` adiciona o contexto de ator/role com reconstrução compatível com SQLite e preservação de histórico. O baseline subiu para 110/110 testes.

## Marco de administração de roles/permissões — 2026-10-17

A área Administração ganhou acesso direto à janela `Roles e permissões`. Ela lista roles ativas/inativas, permite criar novas roles, ativar/desativar e administrar as seis permissões atuais do `PermissionCatalog`. A UI pede confirmação antes de reduzir privilégios e serializa mutações para impedir operações concorrentes na mesma janela.

Antes de expor os botões, `RoleService.SetActiveAsync` e `RevokePermissionAsync` foram endurecidos. O `SqliteAdministrativeAuditStore` usa transação SQLite imediata para verificar a capacidade efetiva restante e executar a mutação com seu audit event. Desativar uma role que sustenta `roles.manage` ou revogar esse grant só é permitido quando outro caminho efetivo permanece. Operação bloqueada não altera estado nem gera evento de sucesso.

Criação de role, ativação/desativação e grant/revogação ganharam eventos canônicos próprios. Para permissão, `DetailCode` contém somente o código validado pelo `PermissionCatalog`. Nenhuma migration nova foi necessária. O baseline automatizado subiu para 117/117 testes.

## Marco de troca de senha pelo próprio usuário — 2026-10-17

O shell ganhou ação `Alterar senha`, abrindo uma janela modal com três `PasswordBox`: senha atual, nova senha e confirmação. Esses valores permanecem fora de ViewModels, sessão, logs e auditoria e são limpos após cada tentativa.

`AccountService.ChangePasswordAsync` é separado do reset administrativo: exige sessão autenticada e senha atual válida, mas não exige `users.manage`. A nova senha passa pela mesma `PasswordPolicy` de 15–128 caracteres. `IUserCredentialStore` abre transação SQLite imediata e revalida que a conta continua ativa e que o hash armazenado ainda é exatamente o hash verificado pelo serviço. Se outro reset/troca alterar a credencial entre leitura e commit, a operação é recusada em vez de sobrescrever a atualização concorrente.

Novo hash, limpeza de `FailedSignInCount`/`SignInBlockedUntilUtc` e `account.password_changed` são persistidos na mesma transação. O evento usa `UserId` e `ActorUserId` iguais à própria conta, sem `RoleId`, `DetailCode`, senha ou hash. Testes reais de SQLite confirmam rollback em falha da auditoria e que duas trocas simultâneas baseadas no mesmo hash antigo resultam em exatamente um commit.

Nenhuma migration nova foi necessária. O baseline automatizado subiu para 127/127 testes (Domain 19, Application 63, Persistence 39, Infrastructure 6), com build WPF Release sem warnings/erros, snapshot EF consistente e seis migrations validadas em apply -> rollback 0 -> reapply.

## Marco de cadastro de empresas — 2026-10-17

A navegação `Cadastros` deixou de apontar apenas para o conteúdo genérico do shell e passou a abrir `CompaniesCatalogView`. A tela lista empresas ativas e inativas, possui estados de carregamento/erro/vazio, refresh explícito, criação por nome, renomeação e ativação/desativação com confirmação antes de reduzir o status.

A UI permanece apenas como orquestração. `CompanyService.ListAsync` continua exigindo `companies.read`, enquanto `CreateAsync`, `RenameAsync` e `SetActiveAsync` continuam exigindo `companies.manage`. Dois testes adicionais de Application registram explicitamente esse contrato. O `Company` existente já suportava nome/status e a persistência EF correspondente, portanto nenhuma migration foi necessária.

O baseline automatizado subiu para 129/129 testes (Domain 19, Application 65, Persistence 39, Infrastructure 6). O CI #102 validou build WPF Release com 0 warnings/0 errors, snapshot EF sem mudanças e as seis migrations em apply -> rollback 0 -> reapply.

Próxima prioridade funcional: concluir o item empresa/filial modelando filial vinculada a `Company`. O próximo slice deve começar apenas pelo relacionamento e por atributos tecnicamente seguros como identidade/nome/status; dados fiscais, endereço e regras específicas dependem de requisitos de negócio e não devem ser presumidos.
