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

## Baseline de autenticação, auditoria e benchmark — 2026-10-16

- Domain: 19/19.
- Application: 33/33.
- Persistence: 24/24.
- Infrastructure: 6/6.
- Total automatizado: 82/82.
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
- Desktop WPF: XAML/code-behind compilados no Windows CI.

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
- fechamento voluntário das janelas;
- foco, Enter, escalonamento/DPI e aparência em tema Windows real.

Também permanece recomendado executar o benchmark de password hashing em uma máquina Windows representativa do hardware alvo e preservar os resultados com CPU, Windows, .NET, média, mediana e P95. A política de retenção de auditoria ainda precisa ser definida, mas não bloqueia a validação técnica já concluída da trilha atual.
