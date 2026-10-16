# Testes

## Estratégia

- Domain: invariantes e value objects;
- Application: regras de caso de uso e autorização;
- Persistence: mappings, constraints, migrations e queries críticas;
- Infrastructure: filesystem, paths e serviços técnicos;
- Desktop: testes focados em ViewModels/commands; UI automation somente em fluxos que justifiquem o custo.

## Regra de bugfix

1. reproduzir;
2. criar teste de regressão quando viável;
3. corrigir;
4. executar teste relacionado;
5. executar suíte relevante.

## Comandos

```powershell
./scripts/test.ps1
```

O script executa restore/build/test em Release.

Ferramentas locais do repositório podem ser restauradas com:

```powershell
dotnet tool restore
```

## Ambientes de validação

`EnableWindowsTargeting=true` permite usar o SDK em Linux/macOS para restore/build de validação do projeto WPF. Isso não substitui o Windows: smoke test, empacotamento e release final devem ser executados no Windows.

## Estado desta sessão

A suíte recebeu testes adicionais de domínio e mapping, mas continua não executada porque o ambiente disponível não possui o .NET SDK. A tentativa de obter o SDK pelo ambiente também foi bloqueada por indisponibilidade de rede no terminal. O backlog mantém restore/build/test como prioridade máxima assim que um SDK estiver disponível.

## RBAC validated baseline (2026-10-16)

Windows Server 2025 with .NET SDK 10.0.402:

- Domain: 13/13 tests passed.
- Application: 20/20 tests passed.
- Persistence: 14/14 tests passed.
- Infrastructure: 6/6 tests passed.
- Total: 53/53 tests passed.
- Release build: 0 warnings / 0 errors.
- EF Core 10.0.13: `has-pending-model-changes` passed.
- Database migration cycle: apply all -> rollback to 0 -> reapply all passed.

