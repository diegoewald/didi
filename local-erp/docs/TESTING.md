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
- Application: 45/45.
- Persistence: 24/24.
- Infrastructure: 6/6.
- Total automatizado: 94/94.
- Build Release: 0 warnings / 0 errors.
- Build da ferramenta de benchmark: 0 warnings / 0 errors.
- EF Core: sem mudanças pendentes no modelo.
- Migrations: 5 migrations; apply -> rollback 0 -> reapply passou.
- Migration de hardening: `20261016131952_AddAuthenticationThrottling`.
- Migration de auditoria: `20261016135247_AddSecurityAuditTrail`.
- Persistência do bootstrap: criação completa, rejeição após usuário existente, rollback em falha e concorrência 1 sucesso/1 rejeição cobertos.
- Throttling: persistência do contador, bloqueio progressivo, expiração e reset em sucesso cobertos.
- Timing: caminhos de usuário ausente, inativo e bloqueado são testados para exigir dummy password verification.
- Auditoria: invariantes de `SecurityAuditEvent`, códigos inválidos, UTC obrigatório, persistência EF, bootstrap e eventos da `AuthenticationService` cobertos.
- Migration smoke: `SecurityAuditEvents` e sua migration fazem parte do ciclo apply/rollback/reapply validado.
- Startup routing: bootstrap, login e acesso ao shell cobertos na camada Application.
- Administração Parte A: `ListAsync(includeInactive: true)` coberto e teste explícito garante demanda de `users.read`.
- Administração Parte B: criação exige `users.manage`; senha curta é rejeitada antes do hash; troca administrativa também rejeita senha abaixo da política; duplicidade normalizada continua coberta.
- Administração Parte C: mudança de status exige `users.manage`, ativa conta inativa, desativa outra conta ativa e rejeita auto-inativação antes de chamar `UpdateAsync`.
- Administração Parte D: reset de senha exige `users.manage`, limpa estado de falhas/lockout sem alterar `IsActive` e rejeita usuário inexistente antes do hashing.
- Desktop WPF: XAML/code-behind, navegação, formulário de criação, ações de status e painel de reset de senha compilados no Windows CI; senhas de criação/reset não passam pelo ViewModel.

Durante a Parte B houve uma falha intermediária de build por `CA1822` em uma propriedade de texto usada por binding. A correção preservou o analyzer ativo e o build subsequente voltou a 0 warnings / 0 errors; nenhuma regra foi desabilitada para obter CI verde.

Na Parte C, um commit intermediário com sintaxe abreviada da exceção foi substituído antes da validação final por uma classe convencional. O run intermediário foi cancelado automaticamente quando a branch avançou; o baseline considera apenas o CI do HEAD final.

Na Parte D, a revisão de UI identificou e corrigiu concorrência possível entre alteração de status e reset de senha. O HEAD funcional final serializa essas ações na View antes da documentação do marco.

## Benchmark Windows CI

Medição de referência executada com o `MicrosoftPasswordHashService` real do ERP, `IterationCount = 220000`, 2 warmups e 20 amostras:

- Hash: min 138.014 ms; mediana 145.155 ms; média 151.692 ms; P95 185.939 ms; max 215.226 ms.
- Verify Success: min 137.500 ms; mediana 138.080 ms; média 146.769 ms; P95 166.364 ms; max 166.375 ms.
- Verify Failure: min 137.247 ms; mediana 141.959 ms; média 146.600 ms; P95 167.560 ms; max 174.817 ms.

O resultado comprova que a ferramenta é executável e fornece baseline real em Windows CI. Ele não representa automaticamente o hardware final do usuário.

## Pendente antes de release

O CI hospedado não substitui teste visual/interativo. Executar smoke manual em Windows verificando:

- primeira abertura sem usuários;
- criação do administrador;
- entrada automática após bootstrap;
- fechamento/reabertura e login;
- credencial inválida;
- comportamento durante throttling/lockout;
- logout e retorno ao login;
- abertura da Administração e renderização da lista de usuários;
- criação de usuário válido e atualização imediata da tabela;
- senha divergente, senha abaixo da política, usuário duplicado e acesso sem `users.manage` na criação;
- limpeza dos dois `PasswordBox` de criação após tentativa;
- desativação de outra conta com confirmação e atualização imediata do status;
- reativação de conta inativa;
- tentativa de auto-inativação exibindo erro sem alterar a conta;
- acesso sem `users.manage` ao alterar status;
- abertura/cancelamento do painel de reset administrativo de senha;
- reset válido de senha para conta ativa e para conta inativa, preservando o status;
- senha divergente, senha abaixo da política, usuário removido durante a tela e acesso sem `users.manage` no reset;
- limpeza dos dois `PasswordBox` de reset após tentativa;
- conta previamente bloqueada conseguindo autenticar com a nova senha após reset administrativo;
- impedir ação concorrente de status enquanto o reset está em andamento e vice-versa;
- estados loading/erro/vazio da administração quando aplicáveis;
- fechamento voluntário das janelas;
- foco, Enter, escalonamento/DPI e aparência em tema Windows real.

Também permanece recomendado executar o benchmark de password hashing em uma máquina Windows representativa do hardware alvo e preservar os resultados com CPU, Windows, .NET, média, mediana e P95. A política de retenção de auditoria ainda precisa ser definida, mas não bloqueia a validação técnica já concluída da trilha atual.
