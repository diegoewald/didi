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
- login retorna falha genérica para credencial inválida;
- o shell principal só abre com sessão autenticada;
- logout limpa a sessão e retorna ao fluxo de login;
- senhas da UI são lidas diretamente de `PasswordBox`, não entram em ViewModels e são limpas após uso.

Ainda pendentes: lockout/throttling, mitigação explícita de diferença de timing entre usuário inexistente e existente, benchmark do custo de hash e eventos de auditoria de segurança.

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

Registrar ações críticas sem armazenar senha, token ou payload sensível integral. A trilha de auditoria ainda é item pendente do próximo ciclo de segurança.

## Dependências

Antes de atualizar/adicionar dependências:

- verificar release estável;
- vulnerabilidades conhecidas;
- manutenção;
- licença;
- breaking changes.

## Baseline validado — 2026-10-16

- bootstrap seguro do primeiro administrador concluído;
- tela real de configuração inicial concluída;
- tela real de login concluída;
- logout com retorno ao login concluído;
- RBAC e autorização deny-by-default ativos;
- build Release Windows: 0 warnings / 0 errors;
- suíte automatizada: 67/67 antes da camada visual, com compilação WPF validada no mesmo CI;
- smoke visual/manual do WPF ainda deve ser feito em máquina Windows interativa antes de release.
