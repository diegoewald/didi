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

## Password hashing e política de senha

A implementação inicial usa `PasswordHasher<TUser>` da Microsoft no formato IdentityV3, encapsulada por `IPasswordHashService`, com `IterationCount = 220000`.

`PasswordPolicy` centraliza a política aplicada ao bootstrap, criação administrativa de usuário, reset administrativo e troca de senha pelo próprio usuário: mínimo de 15 e máximo de 128 caracteres. A validação ocorre na Application antes do hashing, para que a segurança não dependa da UI.

Existe uma ferramenta reproduzível de benchmark em `tools/LocalErp.PasswordHashBenchmark` e o script `scripts/benchmark-password-hashing.ps1`. O benchmark usa o serviço real do ERP e mede hash, verify válido e verify inválido.

Medição de referência feita em GitHub Actions / Windows Server 2025, .NET 10.0.13, AMD64, 4 CPUs, 20 amostras após 2 warmups:

- Hash: mediana 145.155 ms, média 151.692 ms, P95 185.939 ms;
- Verify Success: mediana 138.080 ms, média 146.769 ms, P95 166.364 ms;
- Verify Failure: mediana 141.959 ms, média 146.600 ms, P95 167.560 ms.

Essa medição valida o tooling e fornece baseline Windows, mas não substitui benchmark em hardware representativo do ambiente final. Não reduzir o work factor apenas com base no runner hospedado. Argon2id permanece como alternativa futura caso uma biblioteca .NET madura, mantida e compatível com o projeto seja adotada.

Nunca criar algoritmo de hash próprio.

## Segredos locais

Quando houver necessidade de segredo local, avaliar Windows DPAPI/Credential Manager. Segredos não devem ficar em `appsettings.json` versionado.

## Autenticação local e tratamento de senha na UI

- usernames são normalizados e únicos;
- senha nunca é armazenada em texto puro;
- sessão guarda somente projeção segura do usuário;
- login retorna falha genérica para credencial inválida, conta inexistente, inativa ou temporariamente bloqueada;
- o shell principal só abre com sessão autenticada;
- logout limpa a sessão e retorna ao fluxo de login;
- senhas da UI são lidas diretamente de `PasswordBox` e limpas após uso;
- criação e reset administrativo enviam a senha diretamente da View para `UserService`, sem propriedade ou método de senha no ViewModel;
- a troca própria mantém senha atual, nova senha e confirmação apenas nos `PasswordBox`/parâmetros transitórios da janela e de `AccountService`;
- confirmação de senha permanece restrita à View;
- senha, hash e confirmação de senha não são registrados em logs, sessão ou trilha de auditoria.

## Throttling e lockout

O estado de falhas é persistido por conta em `Users`, usando `FailedSignInCount` e `SignInBlockedUntilUtc`.

Política atual:

- 1ª e 2ª falhas: sem bloqueio temporal;
- 3ª falha: bloqueio de 2 segundos;
- 4ª falha: bloqueio de 10 segundos;
- 5ª falha e seguintes: bloqueio de 15 minutos;
- autenticação bem-sucedida zera contador e bloqueio;
- tentativas durante um bloqueio ativo não aumentam o contador nem prolongam o bloqueio;
- atualização do contador, rehash eventual e reset de sucesso usam transação SQLite não adiada para evitar corrida entre tentativas concorrentes;
- reset administrativo e troca própria de senha bem-sucedida zeram `FailedSignInCount` e `SignInBlockedUntilUtc` junto com a atualização do hash.

## Mitigação de diferença de timing

Caminhos de usuário inexistente, usuário inativo e conta temporariamente bloqueada executam uma verificação de senha dummy usando o mesmo `IPasswordHashService` e o mesmo work factor da aplicação. Isso reduz o oracle de timing mais óbvio entre esses estados e uma senha inválida para conta existente. A técnica é mitigação, não promessa de tempo matematicamente constante de ponta a ponta.

## Bootstrap do primeiro administrador

- não existe usuário ou senha padrão;
- bootstrap só é permitido quando não existe nenhum usuário;
- a operação usa uma única transação SQLite não adiada para adquirir o lock de escrita antes da checagem;
- cria usuário, role `Administrator`, grants e vínculo usuário-role de forma atômica;
- concorrência entre duas inicializações resulta em exatamente um commit;
- falhas de persistência revertem o grafo inteiro;
- após configuração inicial, o caminho especial deixa de existir e o login normal é obrigatório.

## RBAC

- permissões são atribuídas a roles;
- usuários recebem uma ou mais roles;
- operações protegidas usam deny-by-default;
- nomes normalizados de roles são únicos;
- associações user-role e role-permission possuem integridade referencial;
- DTOs de resultado não expõem hash de senha;
- a UI pode ocultar/desabilitar comandos, mas a Application continua sendo a autoridade de autorização;
- listagem administrativa de usuários exige `users.read`;
- criação e demais mutações administrativas de usuário exigem `users.manage`;
- leitura de roles/associações exige `roles.read`;
- criação/status de role, grants/revogações e atribuição/remoção de associações exigem `roles.manage`;
- troca da própria senha não depende de RBAC administrativo: depende de sessão autenticada e prova da senha atual.

## Salvaguardas de status administrativo

`UserService.SetActiveAsync` exige `users.manage` e mantém as regras de status na Application.

- ativação de conta inativa é permitida a operador autorizado;
- desativação de outra conta ativa é permitida a operador autorizado;
- desativação da própria conta autenticada lança `SelfDeactivationNotAllowedException` antes de persistir qualquer alteração;
- a View pede confirmação explícita antes de desativar outra conta, mas essa confirmação é apenas proteção de UX e não substitui a regra da Application.

## Reset administrativo de senha

`UserService.ChangePasswordAsync` exige `users.manage` na Application.

- `PasswordPolicy` de 15–128 caracteres é aplicada antes do hashing;
- o usuário alvo é carregado antes do hash;
- nova senha e confirmação existem apenas nos `PasswordBox`/variáveis transitórias da View e não passam pelo ViewModel;
- os dois `PasswordBox` são limpos após a tentativa, inclusive em erro;
- o novo hash, `FailedSignInCount = 0` e `SignInBlockedUntilUtc = null` são persistidos na mesma atualização;
- `IsActive` é preservado.

## Troca de senha pelo próprio usuário

`AccountService.ChangePasswordAsync` é um caso de uso distinto do reset administrativo. Ele não exige `users.manage`; exige que exista uma sessão autenticada correspondente a uma conta ainda existente e ativa.

- a nova senha passa por `PasswordPolicy` antes de qualquer hash novo;
- a conta persistida é recarregada pelo identificador seguro da sessão;
- a senha atual é verificada com `IPasswordHashService` contra o hash persistido;
- senha atual inválida lança `CurrentPasswordInvalidException` e não gera novo hash, mutação ou evento de sucesso;
- a janela WPF mantém senha atual, nova senha e confirmação em três `PasswordBox`, limpa todos após a tentativa e não usa ViewModel para credenciais;
- após verificar a senha atual, `AccountService` calcula o novo hash e entrega ao `IUserCredentialStore` também o hash anterior exato que foi verificado;
- `SqliteUserCredentialStore` abre transação SQLite imediata (`deferred: false`) e revalida conta ativa + igualdade do hash esperado antes de escrever;
- se reset administrativo ou outra troca de senha alterar a credencial entre a leitura e o lock de escrita, a operação retorna conflito e não sobrescreve a credencial mais recente;
- duas trocas concorrentes partindo do mesmo hash antigo resultam em exatamente um commit;
- novo hash, limpeza do estado de falhas/lockout e evento `account.password_changed` são gravados na mesma transação;
- falha na persistência do audit event reverte a mudança do hash;
- o evento registra `UserId` e `ActorUserId` com o mesmo identificador da conta e mantém `RoleId`/`DetailCode` nulos; nenhuma senha ou hash é persistida na auditoria.

A sessão local atual permanece autenticada após uma troca própria bem-sucedida porque ela contém apenas a projeção segura do usuário e o modelo ainda não possui infraestrutura de sessões distribuídas/revogáveis. Após logout, somente a nova senha deve autenticar.

## Associação de roles e preservação administrativa

A associação e remoção de roles por usuário permanecem fora da lógica de segurança do WPF.

- `RoleService.ListUserAssignmentsAsync` exige `roles.read`;
- `AssignUserAsync` exige `roles.manage`, rejeita usuário inativo e role inativa;
- `RemoveUserAsync` exige `roles.manage`;
- associações existentes podem ser removidas mesmo quando usuário ou role está inativo;
- a capacidade administrativa protegida é `roles.manage`.

A remoção protegida abre transação SQLite imediata (`deferred: false`). Se o vínculo alvo participa de um caminho efetivo de `roles.manage`, a exclusão só continua quando existe outro caminho efetivo após excluir logicamente esse vínculo. “Efetivo” significa usuário ativo + role ativa + grant da permissão. Se a operação eliminaria o último caminho, a persistência retorna `WouldRemoveLastPermissionHolder` e a Application lança `LastAdministrativeRoleAssignmentException`.

A concorrência faz parte da regra: com dois últimos administradores e duas remoções simultâneas, o lock de escrita serializa as decisões. O teste SQLite real confirma exatamente uma remoção permitida, uma rejeitada e uma associação administrativa restante.

## Administração de roles/permissões e preservação administrativa

A UI completa de roles/permissões expõe criação de role, ativação/desativação e grants/revogações das permissões registradas em `PermissionCatalog`. A proteção não depende do nome `Administrator`.

`RoleService.SetActiveAsync` e `RevokePermissionAsync` foram endurecidos antes de serem expostos:

- ativar role não reduz capacidade e é permitido a operador com `roles.manage`;
- desativar role que não sustenta capacidade administrativa efetiva é permitido;
- desativar role que sustenta `roles.manage` é bloqueado quando nenhum outro caminho efetivo permanece;
- revogar permissão diferente de `roles.manage` não aciona a salvaguarda de último administrador;
- revogar `roles.manage` é bloqueado quando a role alvo participa da última capacidade efetiva;
- grants idempotentes e revogações inexistentes são no-op e não geram falso evento de sucesso;
- `LastAdministrativeRoleCapabilityException` representa o bloqueio de desativação/revogação que eliminaria a capacidade final.

A decisão e a mutação são executadas pelo `SqliteAdministrativeAuditStore` em transação SQLite imediata. Para desativação/revogação, a consulta de capacidade restante exclui a role alvo inteira e procura outro caminho composto por usuário ativo + outra role ativa + `roles.manage`. Isso evita check-then-write concorrente. Operação bloqueada faz rollback sem alterar estado nem registrar evento de sucesso.

A janela WPF pede confirmação antes de desativar role ou revogar permissão e impede ações concorrentes enquanto uma mutação está em andamento. Essas confirmações são proteção de UX; a regra de segurança permanece na Application/persistência.

## Auditoria de autenticação

A trilha de auditoria de autenticação está implementada por `SecurityAuditEvent` e `ISecurityAuditStore`, com persistência EF Core em `SecurityAuditEvents`.

Eventos de autenticação:

- `authentication.sign_in.succeeded`;
- `authentication.sign_in.failed`;
- `authentication.sign_in.blocked`;
- `authentication.throttle.applied`;
- `authentication.sign_out`;
- `authentication.bootstrap.succeeded`.

Evento de conta autenticada:

- `account.password_changed`.

Detail codes de autenticação:

- `unknown_account`;
- `inactive_account`;
- `invalid_password`;
- `progressive_delay`;
- `temporary_lockout`;
- `temporarily_blocked`.

Regras de privacidade:

- não persistir senha;
- não persistir hash;
- não persistir token;
- não persistir payload arbitrário;
- não persistir o username digitado quando a conta não existe;
- `UserId` é opcional e só referencia usuário conhecido.

Atomicidade relevante da autenticação/conta:

- senha incorreta no login: atualização do contador e audit event são gravados na mesma transação SQLite;
- login correto: reset de falhas, rehash opcional e audit event são gravados na mesma transação;
- bootstrap: usuário, role, grants, vínculo e audit event são gravados na mesma transação;
- troca própria: novo hash, limpeza de falhas e `account.password_changed` compartilham a mesma transação SQLite imediata;
- logout: a sessão é limpa antes da tentativa de auditoria; falha na auditoria não restaura a sessão.

A migration `20261016135247_AddSecurityAuditTrail` cria `SecurityAuditEvents`, FK opcional para `Users` com `SET NULL` e índices de consulta.

## Auditoria administrativa

`SecurityAuditEvents` também registra contexto normalizado para distinguir ator e alvo sem payload textual arbitrário.

Semântica dos campos:

- `UserId`: usuário conhecido afetado pela operação, quando aplicável;
- `ActorUserId`: usuário autenticado que executou a mutação;
- `RoleId`: role afetada quando aplicável;
- `DetailCode`: código canônico estruturado; para grants/revogações contém somente o permission code validado pelo `PermissionCatalog`.

Eventos administrativos atuais:

- `administration.user.created`;
- `administration.user.activated`;
- `administration.user.deactivated`;
- `administration.user.password_reset`;
- `administration.user_role.assigned`;
- `administration.user_role.removed`;
- `administration.role.created`;
- `administration.role.activated`;
- `administration.role.deactivated`;
- `administration.role_permission.granted`;
- `administration.role_permission.revoked`.

`IAdministrativeAuditStore` concentra a garantia de persistência administrativa:

- criação/status/reset de usuário e evento compartilham a mesma unidade de persistência;
- associação/removal user-role e evento compartilham transação SQLite imediata;
- criação de role e evento compartilham o mesmo `SaveChanges`;
- ativação/desativação de role, grant e revogação de permissão compartilham transação SQLite imediata com o evento;
- no-op idempotente não gera evento de sucesso;
- operação bloqueada pela salvaguarda administrativa não gera evento de sucesso;
- falha de FK da auditoria reverte a mutação associada.

A troca própria usa `IUserCredentialStore`, e não o store administrativo, porque sua autoridade vem da sessão + senha atual em vez de `users.manage`. A garantia de atomicidade com `SecurityAuditEvents` permanece equivalente.

A migration `20261017234500_AddAdministrativeAuditContext` adiciona `ActorUserId` e `RoleId`, ambos opcionais, com `SET NULL`, além de índices por ator/data e role/data. SQLite não suporta adicionar essas FKs posteriormente com `AddForeignKeyOperation`; por isso a migration final reconstrói `SecurityAuditEvents`, copia o histórico e recria FKs/índices. O ciclo apply -> rollback 0 -> reapply foi validado.

Ainda não existe política aprovada de retenção/limpeza da trilha de auditoria. Não excluir eventos automaticamente sem regra de negócio definida.

## Revisão final do slice de auditoria de autenticação — 2026-10-16

A revisão direta do código, migration e testes confirmou atomicidade de falha/sucesso/bootstrap, ausência de senha/hash/token/payload arbitrário e preservação do logout mesmo se seu append de auditoria falhar.

Existe uma dívida de robustez: eventos standalone de conta inexistente, inativa ou já bloqueada são gravados por `ISecurityAuditStore.AppendAsync`; se essa persistência falhar, a exceção atualmente propaga. Isso permanece fail-closed e não cria bypass, mas acopla disponibilidade/UX desses caminhos ao armazenamento de auditoria.

## Baseline validado — 2026-10-17

- bootstrap seguro do primeiro administrador concluído;
- login/logout e RBAC deny-by-default ativos;
- throttling/lockout persistente e mitigação de timing concluídos;
- trilha de auditoria de autenticação concluída;
- administração de usuários Partes A–F concluída;
- administração completa de roles/permissões concluída;
- troca de senha pelo próprio usuário concluída;
- auto-inativação da conta autenticada bloqueada;
- reset administrativo preserva `IsActive` e limpa lockout;
- troca própria exige senha atual, preserva a sessão local e limpa lockout no commit;
- compare-and-set do hash impede sobrescrita concorrente de credencial;
- remoção da última associação que sustenta `roles.manage` bloqueada atomicamente;
- desativação da última role efetivamente administrativa bloqueada atomicamente;
- revogação do último grant efetivo de `roles.manage` bloqueada atomicamente;
- mutações auditadas relevantes persistem evento junto com a alteração de estado e fazem rollback conjunto em falha;
- `ActorUserId`/`RoleId` e `DetailCode` canônico não introduzem payload textual sensível;
- migrations `20261016135247_AddSecurityAuditTrail` e `20261017234500_AddAdministrativeAuditContext` integradas e validadas;
- build Release Windows: 0 warnings / 0 errors;
- suíte automatizada: 127/127 (Domain 19, Application 63, Persistence 39, Infrastructure 6);
- `has-pending-model-changes`: sem mudanças pendentes;
- seis migrations: apply -> rollback 0 -> reapply validado;
- pendentes antes de release: benchmark em hardware Windows representativo e smoke visual/manual do WPF em Windows interativo;
- pendente de governança: política de retenção/limpeza de `SecurityAuditEvents`;
- pendente de robustez: comportamento desejado para falhas de append standalone de auditoria em caminhos fail-closed sem alteração de estado;
- próxima prioridade funcional: Fase 6 — UI operacional de empresa/filial.

## Dependências

Antes de atualizar/adicionar dependências:

- verificar release estável;
- vulnerabilidades conhecidas;
- manutenção;
- licença;
- breaking changes.
