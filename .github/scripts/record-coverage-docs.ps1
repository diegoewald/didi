$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Replace-Exact([string]$Path, [string]$Old, [string]$New) {
    $text = Get-Content $Path -Raw
    if (-not $text.Contains($Old)) { throw "Expected anchor not found in ${Path}: $Old" }
    Set-Content $Path ($text.Replace($Old, $New)) -Encoding utf8NoBOM -NoNewline
}

Replace-Exact 'local-erp/tasks/BACKLOG.md' '- [ ] coverage quando a suíte tiver volume suficiente;' '- [x] coverage automatizado com baseline Cobertura/JSON e validação de coleta, sem threshold percentual arbitrário;'
Replace-Exact 'local-erp/tasks/BACKLOG.md' 'A auditoria NuGet falha diante de qualquer advisory direto ou transitivo conhecido e possui teste sintético do próprio caminho de rejeição.' 'A auditoria NuGet falha diante de qualquer advisory direto ou transitivo conhecido e possui teste sintético do próprio caminho de rejeição. O coverage automatizado gera Cobertura + JSON e registrou 96,76% de linhas (7.578/7.832) nos quatro assemblies exercitados pela suíte; branch coverage não é reportado porque o Cobertura gerado não contém dados reais de branches.'
Replace-Exact 'local-erp/tasks/BACKLOG.md' 'Entre as pendências de engenharia que não exigem nova regra de negócio, a próxima prioridade provável é estabelecer **coverage automatizado com baseline útil**, sem transformar percentual isolado em meta artificial.' 'Entre as pendências de engenharia que não exigem nova regra de negócio, a próxima prioridade provável é melhorar **atalhos, foco e navegação por teclado do shell**, sem iniciar novo slice funcional.'

Replace-Exact 'local-erp/docs/TESTING.md' 'Executa restore, auditoria NuGet direta/transitiva com gate de rejeição sintética, build Release, build de `LocalErp.PasswordHashBenchmark`, testes, smoke WPF, `has-pending-model-changes`, apply de migrations, rollback para zero e reapply.' 'Executa restore, auditoria NuGet direta/transitiva com gate de rejeição sintética, build Release, build de `LocalErp.PasswordHashBenchmark`, testes, coleta de coverage, smoke WPF, `has-pending-model-changes`, apply de migrations, rollback para zero e reapply.'
Replace-Exact 'local-erp/docs/TESTING.md' '- NuGet audit: nenhuma vulnerabilidade conhecida em dependências diretas ou transitivas; fixture sintética vulnerável rejeitada pelo gate;' "- NuGet audit: nenhuma vulnerabilidade conhecida em dependências diretas ou transitivas; fixture sintética vulnerável rejeitada pelo gate;`n- Coverage de linhas: 96,76% (7.578/7.832) em Domain/Application/Infrastructure/Persistence; branch coverage indisponível no Cobertura gerado; sem threshold percentual arbitrário;"
$testing = Get-Content 'local-erp/docs/TESTING.md' -Raw
$anchor = '### Auditoria NuGet automatizada'
if (-not $testing.Contains($anchor)) { throw 'TESTING anchor missing.' }
$section = "### Coverage automatizado`n`n``scripts/coverage.ps1`` usa o coletor Microsoft ``Code Coverage`` e ``dotnet-coverage`` 18.12.0 para gerar ``coverage.cobertura.xml`` e ``coverage-summary.json``, publicados como ``localerp-code-coverage``. O CI #210 validou **96,76% de linhas (7.578/7.832)**: Application 96,34%, Domain 88,18%, Infrastructure 57,50% e Persistence 98,91%. O Desktop WPF não entra nesse relatório e continua validado pelos smokes.`n`nO Cobertura atual não contém dados reais de branches; por isso ``branchDataAvailable=false`` e o valor padrão não é interpretado como 100%. Não há threshold percentual: o gate falha apenas se a coleta estiver ausente, inválida ou sem linhas válidas.`n`n"
Set-Content 'local-erp/docs/TESTING.md' ($testing.Replace($anchor, $section + $anchor)) -Encoding utf8NoBOM -NoNewline

Replace-Exact 'local-erp/README.md' '- CI Windows: build Release com 0 warnings / 0 errors, 271/271 testes, auditoria NuGet direta/transitiva, smoke WPF e smoke do pacote publicado passando' '- CI Windows: build Release com 0 warnings / 0 errors, 271/271 testes, auditoria NuGet direta/transitiva, coverage de linhas 96,76%, smoke WPF e smoke do pacote publicado passando'
Replace-Exact 'local-erp/README.md' 'Benchmark de senha em hardware representativo e verificação automatizada de dependências vulneráveis permanecem pendências próprias.' 'Benchmark de senha em hardware representativo permanece pendência de pré-release; auditoria NuGet e coverage automatizado já integram o CI.'
Replace-Exact 'local-erp/README.md' "./scripts/check-vulnerable-packages.ps1`n./scripts/package-release.ps1 -Configuration Release -Runtime win-x64" "./scripts/check-vulnerable-packages.ps1`n./scripts/coverage.ps1 -Configuration Release`n./scripts/package-release.ps1 -Configuration Release -Runtime win-x64"

Replace-Exact 'local-erp/CHANGELOG.md' '- auditoria automatizada de vulnerabilidades NuGet diretas/transitivas, com parser JSON e teste sintético de rejeição no CI;' "- auditoria automatizada de vulnerabilidades NuGet diretas/transitivas, com parser JSON e teste sintético de rejeição no CI;`n- coverage automatizado com coletor Microsoft, ``dotnet-coverage``, Cobertura/JSON e artifact dedicado, sem threshold percentual arbitrário;"
Replace-Exact 'local-erp/CHANGELOG.md' '- CI técnico #207 validou zero vulnerabilidades NuGet conhecidas, rejeição de uma fixture vulnerável, 271/271 testes e todos os gates de WPF, EF/migrations e package;' "- CI técnico #207 validou zero vulnerabilidades NuGet conhecidas, rejeição de uma fixture vulnerável, 271/271 testes e todos os gates de WPF, EF/migrations e package;`n- CI técnico #210 validou 96,76% de coverage de linhas (7.578/7.832), branches corretamente indisponíveis, 271/271 testes e todos os gates anteriores;"

$roadmap = Get-Content 'local-erp/docs/ROADMAP.md' -Raw
if ($roadmap.Contains('## Marco do coverage automatizado — 2026-10-23')) { throw 'Coverage milestone already exists.' }
$roadmap += "`n`n## Marco do coverage automatizado — 2026-10-23`n`n``scripts/coverage.ps1`` usa o coletor Microsoft ``Code Coverage`` e ``dotnet-coverage`` 18.12.0 para produzir Cobertura/JSON. O CI #210 validou **96,76% de linhas (7.578/7.832)**: Application 96,34%, Domain 88,18%, Infrastructure 57,50% e Persistence 98,91%. Branch coverage é registrado como indisponível porque o relatório não contém dados reais de branches; não há threshold percentual arbitrário. O Desktop permanece validado pelos smokes WPF.`n"
Set-Content 'local-erp/docs/ROADMAP.md' $roadmap -Encoding utf8NoBOM -NoNewline

Replace-Exact 'local-erp/tasks/TECH-DEBT.md' '- O benchmark de password hashing ainda deve ser repetido em hardware Windows representativo.' "- O benchmark de password hashing ainda deve ser repetido em hardware Windows representativo.`n- O coverage automatizado mede Domain/Application/Infrastructure/Persistence, mas não instrumenta o Desktop WPF; a UI continua dependente dos smokes e do smoke visual manual. Branch coverage não está disponível no Cobertura atual e não deve ser inferido como 100%."
Replace-Exact 'local-erp/tasks/BUGS.md' 'auditoria NuGet sem vulnerabilidades conhecidas diretas/transitivas e validação do pacote portátil Windows self-contained com migration bundle.' 'auditoria NuGet sem vulnerabilidades conhecidas diretas/transitivas, coverage de linhas em 96,76% com Cobertura/JSON válido e validação do pacote portátil Windows self-contained com migration bundle.'

git config user.name 'github-actions[bot]'
git config user.email '41898282+github-actions[bot]@users.noreply.github.com'
git add 'local-erp/README.md' 'local-erp/CHANGELOG.md' 'local-erp/docs/TESTING.md' 'local-erp/docs/ROADMAP.md' 'local-erp/tasks/BACKLOG.md' 'local-erp/tasks/BUGS.md' 'local-erp/tasks/TECH-DEBT.md'
git commit -m 'docs(test): record coverage baseline'
git push origin HEAD:erp-coverage-baseline
