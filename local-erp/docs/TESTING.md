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
```

O script executa restore/build/test em Release.

Ferramentas locais:

```powershell
dotnet tool restore
```

## Validação Windows

O CI permanente usa Windows Server 2025, .NET SDK 10.0.402 e `dotnet-ef` 10.0.13. O pipeline executa restore, build Release, testes, `has-pending-model-changes`, apply de migrations, rollback para zero e reapply.

## Baseline de autenticação — 2026-10-16

- Domain: 13/13.
- Application: 30/30.
- Persistence: 18/18.
- Infrastructure: 6/6.
- Total automatizado: 67/67.
- Build Release: 0 warnings / 0 errors.
- EF Core: sem mudanças pendentes no modelo.
- Migrations: apply -> rollback 0 -> reapply passou.
- Persistência do bootstrap: criação completa, rejeição após usuário existente, rollback em falha e concorrência 1 sucesso/1 rejeição cobertos.
- Startup routing: bootstrap, login e acesso ao shell cobertos na camada Application.
- Desktop WPF: XAML/code-behind compilados no Windows CI.

## Pendente antes de release

O CI hospedado não substitui teste visual/interativo. Executar smoke manual em Windows verificando:

- primeira abertura sem usuários;
- criação do administrador;
- entrada automática após bootstrap;
- fechamento/reabertura e login;
- credencial inválida;
- logout e retorno ao login;
- fechamento voluntário das janelas;
- foco, Enter, escalonamento/DPI e aparência em tema Windows real.
