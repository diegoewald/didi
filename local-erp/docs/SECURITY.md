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

A implementação inicial usa `PasswordHasher<TUser>` da Microsoft no formato IdentityV3, encapsulada por `IPasswordHashService`, com `IterationCount = 220000`.

Existe uma ferramenta reproduzível de benchmark em `tools/LocalErp.PasswordHashBenchmark` e o script `scripts/benchmark-password-hashing.ps1`. O benchmark usa o serviço real do ERP e mede hash, verify válido e verify inválido.

Medição de referência feita em GitHub Actions / Windows Server 2025, .NET 10.0.13, AMD64, 4 CPUs, 20 amostras após 2 warmups:

- Hash: mediana 145.155 ms, média 151.692 ms, P95 185.939 ms;
- Verify Success: mediana 138.080 ms, média 146.769 ms, P95 166.364 ms;
- Verify Failure: mediana 141.959 ms, média 146.600 ms, P95 167.560 ms.

Essa medição valida o tooling e fornece baseline Windows, mas não substitui benchmark em hardware representativo do ambiente final. Não reduzir o work factor apenas com base no runner hospedado. Argon2id permanece como alternativa futura caso uma biblioteca .NET madura, mantida e compatível com o projeto seja adotada.

Nunca criar algoritmo de hash próprio.

## Segredos locais

Quando houver necessidade de segredo local, avaliar Windows DPAPI/Credential Manager. Segredos não devem ficar em `appsettings.json` versionado.

## Autenticação local

- usernames são normalizados e únicos;
- senha nunca é armazenada em texto puro;
- sessão guarda somente projeção segura do usuário;
- login retorna falha genérica para credencial inválida, conta inexistente, inativa ou temporariamente bloqueada;
- o shell principal só abre com sessão autenticada;
- logout limpa a sessão e retorna ao fluxo de login;
- senhas da UI são lidas diretamente de `PasswordBox`, não entram em ViewModels e são limpas após uso.

## Throttling e lockout

O estado de falhas é persistido por conta em `Users`, usando `FailedSignInCount` e `SignInBlockedUntilUtc`.

Política atual:

- 1ª e 2ª falhas: sem bloqueio temporal;
- 3ª falha: bloqueio de 2 segundos;
- 4ª falha: bloqueio de 10 segundos;
- 5ª falha e seguintes: bloqueio de 15 minutos;
- autenticação bem-sucedida zera contador e bloqueio;
- tentativas durante um bloqueio ativo não aumentam o contador nem prolongam o bloqueio;
- atualização do contador, rehash eventual e reset de sucesso usam transação SQLite não adiada para evitar corrida entre tentativas concorrentes.

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
- a UI pode ocultar/desabilitar comandos, mas a Application continua sendo a autoridade de autorização.

## Auditoria de autenticação

A trilha de auditoria de autenticação está implementada por `SecurityAuditEvent` e `ISecurityAuditStore`, com persistência EF Core em `SecurityAuditEvents`.

Eventos atuais:

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

Atomicidade relevante:

- senha incorreta: atualização do contador e audit event são gravados na mesma transação SQLite;
- login correto: reset de falhas, rehash opcional e audit event são gravados na mesma transação;
- bootstrap: usuário, role, grants, vínculo e audit event são gravados na mesma transação;
- logout: a sessão é limpa antes da tentativa de auditoria; falha na auditoria é logada, mas não restaura nem mantém a sessão.

A migration `20261016135247_AddSecurityAuditTrail` cria `SecurityAuditEvents`, FK opcional para `Users` com `SET NULL` e índices por data, evento e usuário/data.

Ainda não existe política aprovada de retenção/limpeza da trilha de auditoria. Não excluir eventos automaticamente sem regra de negócio definida. Essa política deve ser fechada antes de crescimento significativo da base.

## Revisão final do slice de auditoria — 2026-10-16

A revisão direta do código, migration e testes confirmou:

- falha de senha: contador, `authentication.sign_in.failed` e eventual `authentication.throttle.applied` compartilham a mesma transação SQLite não adiada;
- login correto: reset de falhas, rehash opcional e `authentication.sign_in.succeeded` compartilham a mesma transação;
- bootstrap: administrador, role, grants, vínculo e `authentication.bootstrap.succeeded` compartilham a mesma transação;
- rollback do bootstrap remove também o evento de auditoria e concorrência gera exatamente um administrador/evento;
- `SecurityAuditEvent` não possui campos para senha, hash, token ou payload textual arbitrário e impõe timestamp UTC/códigos canônicos;
- conta inexistente não persiste o username digitado e usa `UserId = null`;
- FK de auditoria usa `SET NULL`, preservando histórico se um usuário vier a ser excluído no futuro;
- logout limpa a sessão antes do append de auditoria e a UI fecha o shell mesmo se o append falhar.

Não foi encontrado defeito bloqueante neste slice. Existe uma dívida de robustez: os eventos standalone de conta inexistente, inativa ou já bloqueada são gravados por `ISecurityAuditStore.AppendAsync`; se essa persistência falhar, a exceção atualmente propaga. Isso permanece fail-closed e não cria bypass, mas acopla disponibilidade/UX desses caminhos ao armazenamento de auditoria. Avaliar tratamento best-effort ou política explícita antes de release, sem enfraquecer auditoria das operações que alteram estado.

Com essas dívidas registradas, o baseline básico de segurança está suficientemente fechado para iniciar administração de usuários sem exigir que smoke visual, benchmark no hardware alvo, retenção e administração completa sejam falsamente marcados como concluídos.

## Dependências

Antes de atualizar/adicionar dependências:

- verificar release estável;
- vulnerabilidades conhecidas;
- manutenção;
- licença;
- breaking changes.

## Baseline validado — 2026-10-16

- bootstrap seguro do primeiro administrador concluído;
- telas reais de configuração inicial e login concluídas;
- logout com retorno ao login concluído;
- RBAC e autorização deny-by-default ativos;
- throttling/lockout persistente por conta concluído;
- mitigação de timing por dummy password verification concluída;
- trilha de auditoria de autenticação concluída;
- migration `20261016135247_AddSecurityAuditTrail` integrada e validada;
- benchmark reproduzível do hashing concluído no Windows CI;
- build Release Windows: 0 warnings / 0 errors;
- suíte automatizada: 82/82;
- `has-pending-model-changes`: sem mudanças pendentes;
- cinco migrations: apply -> rollback 0 -> reapply validado;
- revisão final do slice de auditoria: concluída sem defeito bloqueante;
- pendentes antes de release: benchmark em hardware Windows representativo e smoke visual/manual do WPF em Windows interativo;
- pendente de governança: política de retenção/limpeza de `SecurityAuditEvents`;
- pendente de robustez: definir comportamento desejado para falhas de append standalone de auditoria em caminhos fail-closed sem alteração de estado.
