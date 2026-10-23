$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Read-Normalized([string]$Path) {
    return ([IO.File]::ReadAllText($Path)).Replace("`r`n", "`n")
}

function Write-Normalized([string]$Path, [string]$Text) {
    [IO.File]::WriteAllText($Path, $Text, [Text.UTF8Encoding]::new($false))
}

function Replace-Exact([string]$Path, [string]$Old, [string]$New) {
    $text = Read-Normalized $Path
    if (-not $text.Contains($Old)) {
        throw "Expected anchor not found in ${Path}: $Old"
    }
    Write-Normalized $Path ($text.Replace($Old, $New))
}

Replace-Exact 'local-erp/tasks/BACKLOG.md' '- [ ] atalhos e foco/teclado do shell;' '- [x] atalhos e foco/teclado do shell com Ctrl+1…8, Ctrl+Tab/Ctrl+Shift+Tab, F6, foco inicial determinístico e navegação direcional na barra lateral;'
Replace-Exact 'local-erp/tasks/BACKLOG.md' 'Baseline automatizado atual: **271/271 testes** (Domain 75, Application 130, Persistence 59, Infrastructure 7), **17 migrations**, build Release e benchmark tool com 0 warnings/erros, smoke WPF automatizado, snapshot EF sem mudanças pendentes e apply -> rollback 0 -> reapply validados no CI Windows. O CI também gera um pacote portátil `win-x64` self-contained com migration bundle, SHA-256, inicializador seguro de banco novo e smoke executado sobre o binário publicado. A auditoria NuGet falha diante de qualquer advisory direto ou transitivo conhecido e possui teste sintético do próprio caminho de rejeição. O coverage automatizado gera Cobertura + JSON e registrou 96,76% de linhas (7.578/7.832) nos quatro assemblies exercitados pela suíte; branch coverage não é reportado porque o Cobertura gerado não contém dados reais de branches.' 'Baseline automatizado atual: **285/285 testes** (Domain 75, Application 130, Persistence 59, Infrastructure 7, Desktop 14), **17 migrations**, build Release e benchmark tool com 0 warnings/erros, smoke WPF automatizado, snapshot EF sem mudanças pendentes e apply -> rollback 0 -> reapply validados no CI Windows. O CI também gera um pacote portátil `win-x64` self-contained com migration bundle, SHA-256, inicializador seguro de banco novo e smoke executado sobre o binário publicado. A auditoria NuGet falha diante de qualquer advisory direto ou transitivo conhecido e possui teste sintético do próprio caminho de rejeição. O coverage automatizado agora inclui também `LocalErp.Desktop` e registrou 73,51% de linhas (7.598/10.336) em cinco assemblies; a mudança de 96,76% para 73,51% decorre da ampliação do denominador para o Desktop, não de perda de cobertura nos quatro assemblies anteriores. Branch coverage continua indisponível no Cobertura gerado.'
Replace-Exact 'local-erp/tasks/BACKLOG.md' 'Entre as pendências de engenharia que não exigem nova regra de negócio, a próxima prioridade provável é melhorar **atalhos, foco e navegação por teclado do shell**, sem iniciar novo slice funcional.' 'Entre as pendências de engenharia que não exigem nova regra de negócio, a próxima candidata é **consolidar componentes loading/empty/error somente onde a repetição justificar**, sem criar abstração prematura nem iniciar novo slice funcional.'

$uxPath = 'local-erp/docs/UX-GUIDELINES.md'
$ux = Read-Normalized $uxPath
if ($ux.Contains('## Navegação por teclado do shell')) { throw 'Shell keyboard UX section already exists.' }
$ux += @'

## Navegação por teclado do shell

- o foco inicial da `MainWindow` vai para a primeira ação da navegação lateral;
- `Ctrl+1` a `Ctrl+8` abrem, respectivamente, Visão geral, Cadastros, Compras, Estoque, Vendas, Financeiro, Relatórios e Administração;
- as mesmas teclas do teclado numérico são aceitas quando combinadas com `Ctrl`;
- setas percorrem ciclicamente os itens da navegação lateral;
- `Ctrl+Tab` e `Ctrl+Shift+Tab` percorrem ciclicamente as abas do módulo ativo em Cadastros e Compras;
- `F6` alterna ciclicamente o foco entre navegação lateral, conteúdo e ações da conta;
- os atalhos `Ctrl+1…8` são expostos por tooltip e `AutomationProperties.HelpText` nos itens da navegação;
- o shell não captura `Esc` globalmente: cancelamento e fechamento continuam pertencendo à tela ou diálogo ativo;
- foco visível, ordem real de tabulação, DPI, teclado físico e ergonomia ainda devem ser conferidos no smoke visual/interativo em Windows real.
'@
Write-Normalized $uxPath $ux

Replace-Exact 'local-erp/docs/TESTING.md' '- Desktop: compilação WPF obrigatória em CI; UI automation somente quando justificar o custo.' '- Desktop: compilação WPF obrigatória, testes determinísticos das regras de teclado/foco e smoke UI Automation; automação adicional somente quando justificar o custo.'
Replace-Exact 'local-erp/docs/TESTING.md' '- Infrastructure: 7/7;`n- Total automatizado: 271/271;' '- Infrastructure: 7/7;`n- Desktop: 14/14;`n- Total automatizado: 285/285;'
Replace-Exact 'local-erp/docs/TESTING.md' '- Coverage de linhas: 96,76% (7.578/7.832) em Domain/Application/Infrastructure/Persistence; branch coverage indisponível no Cobertura gerado; sem threshold percentual arbitrário;' '- Coverage de linhas: 73,51% (7.598/10.336) em Domain/Application/Infrastructure/Persistence/Desktop; branch coverage indisponível no Cobertura gerado; sem threshold percentual arbitrário;'
Replace-Exact 'local-erp/docs/TESTING.md' '`scripts/coverage.ps1` usa o coletor Microsoft `Code Coverage` e `dotnet-coverage` 18.12.0 para gerar `coverage.cobertura.xml` e `coverage-summary.json`, publicados como `localerp-code-coverage`. O CI #210 validou **96,76% de linhas (7.578/7.832)**: Application 96,34%, Domain 88,18%, Infrastructure 57,50% e Persistence 98,91%. O Desktop WPF não entra nesse relatório e continua validado pelos smokes.`n`nO Cobertura atual não contém dados reais de branches; por isso `branchDataAvailable=false` e o valor padrão não é interpretado como 100%. Não há threshold percentual: o gate falha apenas se a coleta estiver ausente, inválida ou sem linhas válidas.' '`scripts/coverage.ps1` usa o coletor Microsoft `Code Coverage` e `dotnet-coverage` 18.12.0 para gerar `coverage.cobertura.xml` e `coverage-summary.json`, publicados como `localerp-code-coverage`. Com `LocalErp.Desktop.Tests` carregando o assembly WPF, o CI técnico #218 passou a medir cinco assemblies e registrou **73,51% de linhas (7.598/10.336)**: Application 96,34%, Desktop 0,80%, Domain 88,18%, Infrastructure 57,50% e Persistence 98,91%. O baseline anterior de 96,76% cobria somente quatro assemblies e, portanto, não é comparável diretamente ao novo total; os percentuais desses quatro assemblies permaneceram estáveis.`n`nO Cobertura atual não contém dados reais de branches; por isso `branchDataAvailable=false` e o valor padrão não é interpretado como 100%. Não há threshold percentual: o gate falha apenas se a coleta estiver ausente, inválida ou sem linhas válidas. O percentual baixo do Desktop reflete que os testes novos cobrem somente as regras determinísticas de navegação por teclado; renderização, integração de foco e fluxo real continuam cobertos por build/smoke e pelo gate manual em Windows.'
$testingPath = 'local-erp/docs/TESTING.md'
$testing = Read-Normalized $testingPath
$smokeAnchor = '### Smoke WPF automatizado'
if (-not $testing.Contains($smokeAnchor)) { throw 'Smoke section anchor not found in TESTING.md.' }
$desktopSection = @'
### Teclado e foco do shell

`LocalErp.Desktop.Tests` adiciona 14 casos determinísticos para o mapeamento de `Ctrl+1…8`, wrap de `Ctrl+Tab`/`Ctrl+Shift+Tab` e ciclo de regiões usado por `F6`. O `MainWindow` continua compilado e inicializado pelos smokes WPF normal e empacotado, evitando validar a regra isolada sem validar também o wiring real do shell.

A automação não injeta teclas físicas no desktop hospedado. O smoke manual em Windows real continua responsável por confirmar foco visível, ordem de tabulação percebida, setas, teclas modificadoras, DPI e ergonomia com teclado real.

'@
$testing = $testing.Replace($smokeAnchor, $desktopSection + $smokeAnchor)
Write-Normalized $testingPath $testing

Replace-Exact 'local-erp/tasks/TECH-DEBT.md' '- O CI Windows agora possui smoke WPF automatizado com banco descartável para bootstrap, autenticação, abertura do shell, navegação pelas abas de Compras e logout. Esse gate não valida ergonomia, foco, DPI nem o fluxo completo com dados e, portanto, não substitui o smoke visual/interativo manual em Windows real.' '- O CI Windows possui smoke WPF automatizado com banco descartável para bootstrap, autenticação, abertura do shell, navegação pelas abas de Compras e logout. Regras determinísticas de atalhos/foco do shell possuem testes próprios, mas o gate não valida ergonomia, foco visível, teclado físico, DPI nem o fluxo completo com dados; portanto, não substitui o smoke visual/interativo manual em Windows real.'
Replace-Exact 'local-erp/tasks/TECH-DEBT.md' '- O coverage automatizado mede Domain/Application/Infrastructure/Persistence, mas não instrumenta o Desktop WPF; a UI continua dependente dos smokes e do smoke visual manual. Branch coverage não está disponível no Cobertura atual e não deve ser inferido como 100%.' '- O coverage automatizado passou a instrumentar Domain/Application/Infrastructure/Persistence/Desktop. O Desktop aparece com cobertura unitária baixa porque somente regras determinísticas de teclado foram extraídas para testes; UI renderizada e ergonomia continuam dependentes dos smokes e do smoke visual manual. Branch coverage não está disponível no Cobertura atual e não deve ser inferido como 100%.'

Replace-Exact 'local-erp/tasks/BUGS.md' 'O ERP é validado pelo `ERP Permanent CI` em Windows Server 2025 com .NET 10. O baseline técnico atual concluiu com sucesso build Release, build da ferramenta de benchmark, **271/271 testes** (Domain 75, Application 130, Persistence 59, Infrastructure 7), smoke WPF automatizado, verificação de snapshot EF, ciclo apply -> rollback 0 -> reapply das **17 migrations**, auditoria NuGet sem vulnerabilidades conhecidas diretas/transitivas, coverage de linhas em 96,76% com Cobertura/JSON válido e validação do pacote portátil Windows self-contained com migration bundle.' 'O ERP é validado pelo `ERP Permanent CI` em Windows Server 2025 com .NET 10. O baseline técnico atual concluiu com sucesso build Release, build da ferramenta de benchmark, **285/285 testes** (Domain 75, Application 130, Persistence 59, Infrastructure 7, Desktop 14), smoke WPF automatizado, verificação de snapshot EF, ciclo apply -> rollback 0 -> reapply das **17 migrations**, auditoria NuGet sem vulnerabilidades conhecidas diretas/transitivas, coverage de linhas em 73,51% sobre cinco assemblies com Cobertura/JSON válido e validação do pacote portátil Windows self-contained com migration bundle.'

Replace-Exact 'local-erp/README.md' '- CI Windows: build Release com 0 warnings / 0 errors, 271/271 testes, auditoria NuGet direta/transitiva, coverage de linhas 96,76%, smoke WPF e smoke do pacote publicado passando' '- CI Windows: build Release com 0 warnings / 0 errors, 285/285 testes, auditoria NuGet direta/transitiva, coverage de linhas 73,51% sobre cinco assemblies, smoke WPF e smoke do pacote publicado passando'
Replace-Exact 'local-erp/README.md' 'O shell também registra falhas não tratadas do dispatcher WPF, do `AppDomain` e de tasks não observadas. Uma exceção fatal no dispatcher mostra somente mensagem genérica ao usuário, registra detalhes técnicos em log e encerra a aplicação para evitar continuar uma sessão potencialmente inconsistente. Isso complementa, e não substitui, tratamento local de erros recuperáveis.' 'O shell também registra falhas não tratadas do dispatcher WPF, do `AppDomain` e de tasks não observadas. Uma exceção fatal no dispatcher mostra somente mensagem genérica ao usuário, registra detalhes técnicos em log e encerra a aplicação para evitar continuar uma sessão potencialmente inconsistente. Isso complementa, e não substitui, tratamento local de erros recuperáveis. A navegação do shell possui foco inicial explícito, `Ctrl+1…8` para seções, `Ctrl+Tab`/`Ctrl+Shift+Tab` para abas e `F6` para alternar regiões de foco.'
Replace-Exact 'local-erp/README.md' '  LocalErp.Persistence.Tests/`ntools/' '  LocalErp.Persistence.Tests/`n  LocalErp.Desktop.Tests/`ntools/'

Replace-Exact 'local-erp/CHANGELOG.md' '- coverage automatizado com coletor Microsoft, `dotnet-coverage`, Cobertura/JSON e artifact dedicado, sem threshold percentual arbitrário;' '- coverage automatizado com coletor Microsoft, `dotnet-coverage`, Cobertura/JSON e artifact dedicado, sem threshold percentual arbitrário;`n- navegação por teclado do shell com `Ctrl+1…8`, `Ctrl+Tab`/`Ctrl+Shift+Tab`, `F6`, foco inicial explícito, setas na barra lateral e testes dedicados do Desktop;'
Replace-Exact 'local-erp/CHANGELOG.md' '- suíte automatizada: 271/271;`n- Domain 75/75, Application 130/130, Persistence 59/59, Infrastructure 7/7;' '- suíte automatizada: 285/285;`n- Domain 75/75, Application 130/130, Persistence 59/59, Infrastructure 7/7, Desktop 14/14;'
Replace-Exact 'local-erp/CHANGELOG.md' '- CI técnico #210 validou 96,76% de coverage de linhas (7.578/7.832), branches corretamente indisponíveis, 271/271 testes e todos os gates anteriores;' '- CI técnico #210 validou 96,76% de coverage de linhas (7.578/7.832), branches corretamente indisponíveis, 271/271 testes e todos os gates anteriores;`n- CI técnico #218 validou a navegação por teclado do shell com 285/285 testes, build sem warnings/erros, coverage de 73,51% agora incluindo Desktop, dois smokes WPF, snapshot EF, 17 migrations e pacote QA;'

$roadmapPath = 'local-erp/docs/ROADMAP.md'
$roadmap = Read-Normalized $roadmapPath
if ($roadmap.Contains('## Marco de navegação por teclado do shell — 2026-10-23')) { throw 'Shell keyboard roadmap milestone already exists.' }
$roadmap += @'

## Marco de navegação por teclado do shell — 2026-10-23

O shell WPF passou a ter foco inicial determinístico na navegação lateral, atalhos globais `Ctrl+1…8` para as oito seções, navegação direcional cíclica na barra lateral, `Ctrl+Tab`/`Ctrl+Shift+Tab` para percorrer abas de Cadastros/Compras e `F6` para alternar entre navegação, conteúdo e ações da conta. Os atalhos de seção também ficam expostos em tooltip e `AutomationProperties.HelpText`.

As regras determinísticas foram isoladas em `ShellKeyboardNavigation` e cobertas por `LocalErp.Desktop.Tests`, acrescentando 14 testes. O CI técnico #218 fechou com **285/285 testes**, build Release/benchmark sem warnings ou erros, auditoria NuGet limpa, smoke WPF normal, snapshot EF limpo, 17 migrations em apply -> rollback 0 -> reapply, pacote QA e smoke do binário publicado.

Ao carregar o assembly Desktop na coleta, o coverage total passou de 96,76% em quatro assemblies para **73,51% (7.598/10.336) em cinco assemblies**. Os quatro percentuais anteriores permaneceram estáveis; `LocalErp.Desktop` aparece com 0,80% porque somente a lógica determinística extraída é unitariamente testada. Essa diferença é registrada como ampliação honesta do escopo medido, não como regressão funcional. A ergonomia real de foco/teclado continua no smoke visual manual de pré-release.
'@
Write-Normalized $roadmapPath $roadmap

git config user.name 'github-actions[bot]'
git config user.email '41898282+github-actions[bot]@users.noreply.github.com'
git add local-erp/README.md local-erp/CHANGELOG.md local-erp/docs/ROADMAP.md local-erp/docs/TESTING.md local-erp/docs/UX-GUIDELINES.md local-erp/tasks/BACKLOG.md local-erp/tasks/BUGS.md local-erp/tasks/TECH-DEBT.md
git commit -m 'docs(shell): record keyboard navigation milestone'
git push origin HEAD:erp-shell-keyboard
