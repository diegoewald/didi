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

A implementação inicial usa `PasswordHasher<TUser>` da Microsoft no formato IdentityV3, encapsulada por `IPasswordHashService`. O custo precisa continuar sendo benchmarkado em hardware Windows representativo antes de release de produção. Argon2id permanece como alternativa futura caso uma biblioteca .NET madura, mantida e compatível com o projeto seja adotada.

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

## Auditoria

Registrar ações críticas sem armazenar senha, token ou payload sensível integral. A trilha de auditoria de autenticação ainda é item pendente do próximo ciclo de segurança.

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
- migration `20261016131952_AddAuthenticationThrottling` gerada pelo EF Core e validada;
- build Release Windows: 0 warnings / 0 errors;
- suíte automatizada: 76/76;
- `has-pending-model-changes`: sem mudanças pendentes;
- quatro migrations: apply -> rollback 0 -> reapply validado;
- pendentes antes de release: benchmark do hashing, trilha de auditoria e smoke visual/manual do WPF em Windows interativo.
