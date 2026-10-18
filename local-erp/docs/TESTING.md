# Testes

## Estratégia

- Domain: invariantes e value objects;
- Application: casos de uso, autenticação, bootstrap e autorização;
- Persistence: mappings, constraints, migrations, concorrência e queries críticas;
- Infrastructure: filesystem, paths e serviços técnicos;
- Desktop: compilação WPF obrigatória em CI; UI automation somente quando justificar o custo.

## Regra de bugfix

1. reproduzir;
2. criar teste de regressão quando viável;
3. corrigir;
4. executar teste relacionado;
5. executar suíte relevante.

## Validação Windows

O CI permanente usa Windows Server 2025, .NET SDK 10.0.402 e `dotnet-ef` 10.0.13. Executa restore, build Release, build de `LocalErp.PasswordHashBenchmark`, testes, `has-pending-model-changes`, apply de migrations, rollback para zero e reapply.

## Baseline atual — 2026-10-18

- Domain: 22/22;
- Application: 69/69;
- Persistence: 41/41;
- Infrastructure: 6/6;
- Total automatizado: 138/138;
- Build Release: 0 warnings / 0 errors;
- Benchmark tool: 0 warnings / 0 errors;
- EF Core: sem mudanças pendentes no modelo;
- Migrations: 7; apply -> rollback 0 -> reapply passando.

### Cobertura de empresa/filial

- domínio de `CompanyBranch`: normalização do nome, `CompanyId` obrigatório, rename e status;
- Application: criação exige empresa existente; listagem exige `companies.read`; criação/rename/status exigem `companies.manage`;
- listagem de filiais fica restrita à empresa solicitada;
- Persistence: insert/list/filter/update por empresa e status;
- FK SQLite rejeita filial apontando para empresa inexistente;
- migration `20261018105000_AddCompanyBranches` entra no ciclo completo de migration smoke;
- CI #104 validou backend, snapshot e 7 migrations;
- CI #105 validou a janela WPF `CompanyBranchesWindow` e o botão `Gerenciar` no catálogo de Empresas sobre o mesmo baseline.

### Cobertura de segurança mantida

O baseline continua cobrindo bootstrap concorrente, throttling/lockout, dummy verify, auditoria de autenticação, administração de usuários, salvaguardas de `roles.manage`, auditoria administrativa, troca própria de senha e compare-and-set concorrente de credenciais.

## Benchmark Windows CI

Referência com `MicrosoftPasswordHashService`, `IterationCount = 220000`, 2 warmups e 20 amostras:

- Hash: mediana 145.155 ms;
- Verify Success: mediana 138.080 ms;
- Verify Failure: mediana 141.959 ms.

Esses números validam o tooling, não representam automaticamente o hardware final.

## Pendente antes de release

Executar smoke manual em Windows incluindo:

- bootstrap/login/logout e throttling;
- Administração de usuários/roles/permissões;
- troca própria de senha e limpeza dos `PasswordBox`;
- catálogo de Empresas: loading/erro/vazio, criação, rename e status;
- botão `Gerenciar` de cada empresa e abertura da janela de Filiais;
- Filiais: lista ativa/inativa, criação, rename, ativação/desativação e confirmação;
- verificar que desativar empresa não modifica automaticamente filiais;
- permissões insuficientes para `companies.read/manage` exibindo erro sem bypass;
- foco, Enter, fechamento durante mutação, DPI e aparência em Windows real;
- benchmark de senha em hardware representativo.
