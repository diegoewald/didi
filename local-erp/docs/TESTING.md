# Testes

## Estratégia

- Domain: invariantes e value objects;
- Application: casos de uso, autenticação, bootstrap e autorização;
- Persistence: mappings, constraints, migrations, concorrência e queries críticas;
- Infrastructure: filesystem, paths e serviços técnicos;
- Desktop: compilação WPF obrigatória em CI; testes focados em ViewModels/commands quando houver lógica relevante; UI automation somente quando justificar o custo.

## Regra de bugfix

1. reproduzir;
2. criar teste de regressão quando viável;
3. corrigir;
4. executar teste relacionado;
5. executar suíte relevante.

## Comandos

```powershell
./scripts/test.ps1
./scripts/benchmark-password-hashing.ps1
```

O script de testes executa restore/build/test em Release. O script de benchmark executa a ferramenta real de password hashing e produz medições reproduzíveis para hash, verify válido e verify inválido.

Ferramentas locais:

```powershell
dotnet tool restore
```

## Validação Windows

O CI permanente usa Windows Server 2025, .NET SDK 10.0.402 e `dotnet-ef` 10.0.13. O pipeline executa restore, build Release, build da ferramenta `LocalErp.PasswordHashBenchmark`, testes, `has-pending-model-changes`, apply de migrations, rollback para zero e reapply.

## Baseline atual — 2026-10-17

- Domain: 19/19.
- Application: 63/63.
- Persistence: 39/39.
- Infrastructure: 6/6.
- Total automatizado: 127/127.
- Build Release: 0 warnings / 0 errors.
- Build da ferramenta de benchmark: 0 warnings / 0 errors.
- EF Core: sem mudanças pendentes no modelo.
- Migrations: 6 migrations; apply -> rollback 0 -> reapply passou.
- Migration de hardening: `20261016131952_AddAuthenticationThrottling`.
- Migration de auditoria de autenticação: `20261016135247_AddSecurityAuditTrail`.
- Migration de contexto administrativo: `20261017234500_AddAdministrativeAuditContext`.
- Persistência do bootstrap: criação completa, rejeição após usuário existente, rollback em falha e concorrência 1 sucesso/1 rejeição cobertos.
- Throttling: persistência do contador, bloqueio progressivo, expiração e reset em sucesso cobertos.
- Timing: caminhos de usuário ausente, inativo e bloqueado são testados para exigir dummy password verification.
- Auditoria de autenticação: invariantes de `SecurityAuditEvent`, códigos inválidos, UTC obrigatório, persistência EF, bootstrap e eventos da `AuthenticationService` cobertos.
- Startup routing: bootstrap, login e acesso ao shell cobertos na camada Application.
- Administração Parte A: `ListAsync(includeInactive: true)` e exigência de `users.read` cobertos.
- Administração Parte B: criação exige `users.manage`; senha curta é rejeitada antes do hash; duplicidade normalizada continua coberta.
- Administração Parte C: mudança de status exige `users.manage` e auto-inativação é rejeitada antes de persistir.
- Administração Parte D: reset exige `users.manage`, limpa lockout sem alterar `IsActive` e rejeita usuário inexistente antes do hashing.
- Administração Parte E: leitura de associações exige `roles.read`; atribuição/remoção exigem `roles.manage`; remoção do último `roles.manage` efetivo é bloqueada.
- Concorrência Parte E: duas remoções simultâneas dos dois últimos administradores deixam exatamente uma associação administrativa.
- Administração Parte F: criação, status, reset e associação/remoção de role validam evento, ator e alvo; falha da FK de auditoria reverte a mutação.
- Administração de roles/permissões: criação de role e grant auditados; desativação da última role administrativa e revogação do último `roles.manage` efetivo são bloqueadas.
- Troca própria de senha — Application: sessão autenticada, senha atual válida, política da nova senha, evento `account.password_changed` e conflito de credencial antes do commit estão cobertos.
- Troca própria de senha — Persistence: novo hash, reset de lockout e evento são persistidos juntos; hash esperado obsoleto e conta inativa são recusados sem evento; falha da auditoria reverte o hash.
- Concorrência de credencial: duas trocas simultâneas baseadas no mesmo hash antigo resultam em exatamente um sucesso, um conflito e um único evento persistido.
- Migration smoke: `ActorUserId` e `RoleId` são exigidos após apply/reapply e o rollback completo das seis migrations passa.
- Desktop WPF: XAML/code-behind da administração de usuários, janela de associações, janela completa de roles/permissões e janela `Alterar senha` compilam no Windows CI.

Durante a Parte B houve uma falha intermediária de build por `CA1822`; a correção preservou o analyzer ativo. Na Parte D, a revisão de UI corrigiu concorrência entre status e reset. Na Parte E, a salvaguarda do último administrador foi validada com SQLite real e transação não adiada.

Na Parte F, o run funcional intermediário #94 falhou porque a primeira versão da migration tentou `AddForeignKeyOperation` sobre tabela SQLite existente. A correção reconstruiu `SecurityAuditEvents`, preservou registros/FKs e o run #95 validou o ciclo completo.

Na administração completa de roles/permissões, a salvaguarda foi implementada antes da UI. O run #97 validou as transações protegidas; o run #98 validou o mesmo baseline junto com o novo XAML/code-behind WPF.

Na troca própria de senha, o run #100 validou 127/127 testes. O teste concorrente do `SqliteUserCredentialStore` comprova o compare-and-set sob transação imediata: duas operações partindo do mesmo hash antigo não conseguem sobrescrever uma à outra. Nenhuma migration nova foi necessária.

## Benchmark Windows CI

Medição de referência executada com o `MicrosoftPasswordHashService` real do ERP, `IterationCount = 220000`, 2 warmups e 20 amostras:

- Hash: min 138.014 ms; mediana 145.155 ms; média 151.692 ms; P95 185.939 ms; max 215.226 ms.
- Verify Success: min 137.500 ms; mediana 138.080 ms; média 146.769 ms; P95 166.364 ms; max 166.375 ms.
- Verify Failure: min 137.247 ms; mediana 141.959 ms; média 146.600 ms; P95 167.560 ms; max 174.817 ms.

O resultado comprova que a ferramenta é executável e fornece baseline real em Windows CI. Ele não representa automaticamente o hardware final do usuário.

## Pendente antes de release

O CI hospedado não substitui teste visual/interativo. Executar smoke manual em Windows verificando:

- primeira abertura sem usuários, bootstrap, reabertura/login e logout;
- credencial inválida e comportamento durante throttling/lockout;
- abertura da Administração e renderização da lista de usuários;
- criação/status/reset de usuário, incluindo erros e limpeza de `PasswordBox`;
- conta previamente bloqueada autenticando com a nova senha após reset administrativo;
- serialização de status/reset na tela;
- abertura da janela de roles de usuário, associação/removal, roles inativas e permissões insuficientes;
- self-demotion permitida quando outro `roles.manage` efetivo permanece e bloqueada quando seria a última;
- abertura da janela `Roles e permissões` pelo botão direto da Administração;
- criação de role e rejeição visual de nome duplicado/inválido;
- ativação e desativação de roles com confirmação;
- concessão e revogação de cada permission code do catálogo;
- aviso reforçado ao revogar `roles.manage`;
- bloqueio visual ao tentar desativar a última role administrativa efetiva;
- bloqueio visual ao tentar revogar o último `roles.manage` efetivo;
- operação equivalente permitida quando outro caminho administrativo efetivo permanece;
- fechamento/refresh da janela impedidos durante mutação em andamento;
- botão `Alterar senha` disponível para usuário autenticado independentemente de `users.manage`;
- senha atual incorreta exibindo erro sem alterar a credencial;
- nova senha divergente ou abaixo da política sendo recusada e todos os `PasswordBox` sendo limpos;
- troca própria válida mantendo a sessão local atual, seguida de logout: senha antiga deve falhar e nova senha deve autenticar;
- conta com lockout anterior tendo esse estado limpo após troca própria bem-sucedida;
- verificar em banco/log de diagnóstico controlado que mutações auditadas bem-sucedidas produzem eventos estruturados corretos e sem senha/hash;
- confirmar que no-op, operação bloqueada ou conflito de credencial não produzem falso evento de sucesso;
- estados loading/erro/vazio quando aplicáveis;
- foco, Enter, escalonamento/DPI e aparência em tema Windows real.

Também permanece recomendado executar o benchmark de password hashing em uma máquina Windows representativa do hardware alvo e preservar os resultados com CPU, Windows, .NET, média, mediana e P95. A política de retenção de auditoria ainda precisa ser definida.
