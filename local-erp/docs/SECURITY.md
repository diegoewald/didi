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

`PasswordPolicy` centraliza a política atualmente aplicada ao bootstrap, criação administrativa de usuário e troca administrativa de senha: mínimo de 15 e máximo de 128 caracteres. A validação ocorre na Application antes do hashing, para que a segurança não dependa da UI.

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
- confirmação de senha também permanece restrita aos `PasswordBox` da View;
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
- o reset administrativo de senha também zera `FailedSignInCount` e `SignInBlockedUntilUtc` na mesma atualização do usuário, para que a recuperação da credencial não deixe um lockout anterior ativo.

A política é deliberadamente vinculada à conta. O bloqueio progressivo curto reduz brute force sem transformar cada tentativa durante um lockout em extensão indefinida, o que diminuiria resistência a negação de serviço direcionada.

## Mitigação de diferença de timing

Caminhos de usuário inexistente, usuário inativo e conta temporariamente bloqueada executam uma verificação de senha dummy usando o mesmo `IPasswordHashService` e o mesmo work factor da aplicação. Isso remove o retorno antecipado antes do cálculo caro do hash e reduz o oracle de timing mais óbvio entre esses estados e uma senha inválida para conta existente.

Essa técnica é uma mitigação, não uma promessa de tempo de execução matematicamente constante de ponta a ponta: consultas ao banco, escalonamento do sistema operacional e outras fontes de variação ainda existem. O contrato público continua sendo uma única falha genérica.

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
- criação e demais mutações administrativas existentes em `UserService` exigem `users.manage`;
- leitura de associações usuário-role exige `roles.read`;
- atribuição e remoção de associações usuário-role exigem `roles.manage`.

## Salvaguardas de status administrativo

`UserService.SetActiveAsync` exige `users.manage` e mantém as regras de status na Application.

- ativação de conta inativa é permitida a operador autorizado;
- desativação de outra conta ativa é permitida a operador autorizado;
- desativação da própria conta autenticada lança `SelfDeactivationNotAllowedException` antes de persistir qualquer alteração;
- a View pede confirmação explícita antes de desativar outra conta, mas essa confirmação é apenas proteção de UX e não substitui a regra da Application;
- a reativação não depende de regra no WPF e também passa por `users.manage`.

A auto-inativação permanece bloqueada porque poderia remover imediatamente a capacidade do operador atual. A proteção de último administrador para mutações de roles é tratada separadamente na Parte E, com base em capacidade efetiva e não apenas no nome de uma role.

## Reset administrativo de senha

`UserService.ChangePasswordAsync` é o caso de uso de recuperação administrativa de senha e exige `users.manage` na Application.

- `PasswordPolicy` de 15–128 caracteres é aplicada antes do hashing;
- o usuário alvo é carregado antes do hash; conta inexistente falha sem executar hashing desnecessário;
- nova senha e confirmação existem apenas nos `PasswordBox`/variáveis transitórias da View e não passam pelo ViewModel;
- os dois `PasswordBox` são limpos após a tentativa, inclusive em erro;
- o novo hash, `FailedSignInCount = 0` e `SignInBlockedUntilUtc = null` são persistidos na mesma atualização da entidade `User`;
- `IsActive` é preservado: redefinir a senha de uma conta inativa não a reativa implicitamente;
- a View impede que reset de senha e alteração de status sejam iniciados concorrentemente, reduzindo ambiguidade de UX; essa serialização não substitui autorização nem invariantes da Application.

Ainda não existe invalidação de sessões administrativas após troca de senha porque o modelo atual mantém apenas a sessão local em memória da instância em execução e não possui infraestrutura de sessões distribuídas/revogáveis. Não inferir uma capacidade de revogação que ainda não existe.

## Associação de roles e preservação administrativa

A Parte E expõe associação e remoção de roles por usuário sem mover regras de segurança para o WPF.

- `RoleService.ListUserAssignmentsAsync` exige `roles.read` e valida que o usuário alvo existe;
- `RoleService.AssignUserAsync` exige `roles.manage`, rejeita usuário inativo e rejeita role inativa;
- `RoleService.RemoveUserAsync` exige `roles.manage` e valida usuário/role antes de solicitar a remoção protegida;
- associações já existentes podem ser removidas mesmo quando usuário ou role está inativo, permitindo limpeza administrativa sem conceder nova capacidade;
- a capacidade administrativa protegida neste slice é `roles.manage`, porque ela autoriza as mutações de RBAC expostas pela Parte E.

A remoção não usa uma checagem seguida de delete em contextos independentes. A persistência abre uma transação SQLite imediata (`deferred: false`), obtendo serialização de escrita antes de avaliar a regra. Se o vínculo alvo participa de um caminho efetivo de `roles.manage`, a operação só continua quando existe outro caminho efetivo após excluir logicamente esse vínculo. “Efetivo” segue a mesma semântica da autorização: usuário ativo + role ativa + grant da permissão.

Se a remoção eliminaria o último caminho, o repositório/store faz rollback e retorna `WouldRemoveLastPermissionHolder`; a Application converte esse resultado em `LastAdministrativeRoleAssignmentException`. Isso permite self-demotion quando outro administrador efetivo permanece e bloqueia a self-demotion quando ela eliminaria a capacidade administrativa final.

A concorrência também é parte da regra: com dois últimos administradores e duas remoções simultâneas, a transação imediata serializa as decisões. O teste SQLite real confirma exatamente uma remoção permitida, uma rejeitada e uma associação administrativa restante.

Escopo importante: este slice protege a remoção de associação usuário↔role, que é a mutação de RBAC atualmente exposta na Administração de usuários. `RoleService.SetActiveAsync` e `RoleService.RevokePermissionAsync` já existem como APIs internas do domínio de aplicação, mas ainda não são expostas pela UI completa de roles/permissões. Antes de expor desativação de role ou revogação de `roles.manage`, essas mutações deverão receber salvaguarda equivalente para não criar uma rota alternativa de perda da última capacidade administrativa.

## Auditoria de autenticação

A trilha de auditoria de autenticação está implementada por `SecurityAuditEvent` e `ISecurityAuditStore`, com persistência EF Core em `SecurityAuditEvents`.

Eventos atuais de autenticação:

- `authentication.sign_in.succeeded`;
- `authentication.sign_in.failed`;
- `authentication.sign_in.blocked`;
- `authentication.throttle.applied`;
- `authentication.sign_out`;
- `authentication.bootstrap.succeeded`.

Detail codes atuais:

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

Atomicidade relevante da autenticação:

- senha incorreta: atualização do contador e audit event são gravados na mesma transação SQLite;
- login correto: reset de falhas, rehash opcional e audit event são gravados na mesma transação;
- bootstrap: usuário, role, grants, vínculo e audit event são gravados na mesma transação;
- logout: a sessão é limpa antes da tentativa de auditoria; falha na auditoria é logada, mas não restaura nem mantém a sessão.

A migration `20261016135247_AddSecurityAuditTrail` cria `SecurityAuditEvents`, FK opcional para `Users` com `SET NULL` e índices por data, evento e usuário/data.

## Auditoria administrativa

A Parte F reutiliza `SecurityAuditEvents`, mas adiciona contexto normalizado para distinguir ator e alvo sem introduzir payload textual arbitrário.

Semântica dos campos:

- `UserId`: usuário conhecido afetado pela operação administrativa; nos eventos de autenticação continua representando a conta conhecida associada ao evento;
- `ActorUserId`: usuário autenticado que executou a mutação administrativa; permanece `NULL` para os eventos de autenticação que não precisam desse segundo papel;
- `RoleId`: role afetada por associação/remoção; `NULL` quando a operação não envolve role;
- `DetailCode`: continua restrito a códigos canônicos e não é usado para transportar senha, username, descrição livre ou payload operacional.

Eventos administrativos atuais:

- `administration.user.created`;
- `administration.user.activated`;
- `administration.user.deactivated`;
- `administration.user.password_reset`;
- `administration.user_role.assigned`;
- `administration.user_role.removed`.

`IAdministrativeAuditStore` concentra a garantia de persistência:

- criação de usuário: novo `User` e audit event compartilham um `SaveChanges` transacional;
- ativação/desativação: alteração de `User` e audit event compartilham um `SaveChanges`;
- reset administrativo: novo hash, limpeza de lockout e audit event compartilham um `SaveChanges`;
- associação user-role: vínculo e audit event compartilham transação SQLite imediata;
- remoção user-role: salvaguarda do último `roles.manage`, exclusão e audit event compartilham a mesma transação SQLite imediata;
- no-op idempotente de associação/remoção não gera falso evento de sucesso;
- operação bloqueada pela salvaguarda do último administrador não gera evento de remoção bem-sucedida.

Os testes de persistência forçam `ActorUserId` inválido para provocar erro de FK e confirmam que a mudança de estado também é revertida. O teste concorrente de remoção valida simultaneamente a regra de último administrador e a auditoria: apenas a remoção commitada gera evento.

A migration `20261017234500_AddAdministrativeAuditContext` adiciona `ActorUserId` e `RoleId`, ambos opcionais, com `SET NULL` para preservar o histórico caso usuário/role seja excluído futuramente, além de índices por ator/data e role/data. SQLite não suporta adicionar essas FKs posteriormente com `AddForeignKeyOperation`; por isso a migration final reconstrói `SecurityAuditEvents`, copia todos os eventos existentes com os novos campos nulos e recria FKs/índices. O ciclo apply -> rollback 0 -> reapply foi validado.

Escopo: a auditoria administrativa atual cobre as mutações expostas pela Administração de usuários — criação, status, reset e associação/remoção de role. `RoleService.SetActiveAsync` e `RoleService.RevokePermissionAsync` ainda não fazem parte da UI completa de roles/permissões; antes de expô-las, elas deverão receber tanto a salvaguarda de última capacidade administrativa quanto auditoria atômica própria.

Ainda não existe política aprovada de retenção/limpeza da trilha de auditoria. Não excluir eventos automaticamente sem regra de negócio definida. Essa política deve ser fechada antes de crescimento significativo da base.

## Revisão final do slice de auditoria de autenticação — 2026-10-16

A revisão direta do código, migration e testes confirmou:

- falha de senha: contador, `authentication.sign_in.failed` e eventual `authentication.throttle.applied` compartilham a mesma transação SQLite não adiada;
- login correto: reset de falhas, rehash opcional e `authentication.sign_in.succeeded` compartilham a mesma transação;
- bootstrap: administrador, role, grants, vínculo e `authentication.bootstrap.succeeded` compartilham a mesma transação;
- rollback do bootstrap remove também o evento de auditoria e concorrência gera exatamente um administrador/evento;
- `SecurityAuditEvent` não possui campos para senha, hash, token ou payload textual arbitrário e impõe timestamp UTC/códigos canônicos;
- conta inexistente não persiste o username digitado e usa `UserId = null`;
- FKs de auditoria usam `SET NULL`, preservando histórico se entidades referenciadas vierem a ser excluídas;
- logout limpa a sessão antes do append de auditoria e a UI fecha o shell mesmo se o append falhar.

Não foi encontrado defeito bloqueante neste slice. Existe uma dívida de robustez: os eventos standalone de conta inexistente, inativa ou já bloqueada são gravados por `ISecurityAuditStore.AppendAsync`; se essa persistência falhar, a exceção atualmente propaga. Isso permanece fail-closed e não cria bypass, mas acopla disponibilidade/UX desses caminhos ao armazenamento de auditoria. Avaliar tratamento best-effort ou política explícita antes de release, sem enfraquecer auditoria das operações que alteram estado.

## Baseline validado — 2026-10-17

- bootstrap seguro do primeiro administrador concluído;
- telas reais de configuração inicial e login concluídas;
- logout com retorno ao login concluído;
- RBAC e autorização deny-by-default ativos;
- throttling/lockout persistente por conta concluído;
- mitigação de timing por dummy password verification concluída;
- trilha de auditoria de autenticação concluída;
- administração de usuários Partes A–F concluídas: listagem, criação, ativação/desativação, reset administrativo, associação de roles e auditoria administrativa;
- auto-inativação da conta autenticada bloqueada na Application;
- reset administrativo limpa tentativas falhas/bloqueio temporário sem reativar conta inativa;
- remoção da última associação que sustenta `roles.manage` efetivo bloqueada atomicamente;
- teste concorrente de remoção administrativa validado em SQLite real;
- mutações administrativas expostas persistem evento junto com a alteração de estado e fazem rollback conjunto em falha;
- `ActorUserId`/`RoleId` não introduzem payload textual sensível e usam FKs opcionais `SET NULL`;
- política de senha central aplicada a bootstrap, criação e troca administrativa;
- migrations `20261016135247_AddSecurityAuditTrail` e `20261017234500_AddAdministrativeAuditContext` integradas e validadas;
- benchmark reproduzível do hashing concluído no Windows CI;
- build Release Windows: 0 warnings / 0 errors;
- suíte automatizada: 110/110;
- `has-pending-model-changes`: sem mudanças pendentes;
- seis migrations: apply -> rollback 0 -> reapply validado;
- pendentes antes de release: benchmark em hardware Windows representativo e smoke visual/manual do WPF em Windows interativo;
- pendente de governança: política de retenção/limpeza de `SecurityAuditEvents`;
- pendente de robustez: definir comportamento desejado para falhas de append standalone de auditoria em caminhos fail-closed sem alteração de estado;
- pendente funcional: administração completa de roles/permissões e troca de senha pelo próprio usuário;
- pendente para a futura UI completa de roles/permissões: aplicar proteção de última capacidade e auditoria atômica a desativação de role/revogação de `roles.manage`.

## Dependências

Antes de atualizar/adicionar dependências:

- verificar release estável;
- vulnerabilidades conhecidas;
- manutenção;
- licença;
- breaking changes.
