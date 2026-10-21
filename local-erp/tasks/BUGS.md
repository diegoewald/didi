# Bugs

Nenhum bug funcional confirmado no baseline atual.

## Validação atual

O ERP é validado pelo `ERP Permanent CI` em Windows Server 2025 com .NET 10. O HEAD funcional mais recente concluiu com sucesso build Release, build da ferramenta de benchmark, 198/198 testes, verificação de snapshot EF e ciclo apply -> rollback 0 -> reapply das treze migrations.

O cadastro de Centro de Custo está integrado com `cost-centers.read/manage` na Application, persistência EF, migration `20261021100000_AddCostCenters` e catálogo WPF no módulo `Cadastros`. O CI #131 recompilou a UI de Centros de Custo e preservou todo o baseline automatizado.

Categoria e Unidade de Medida permanecem integradas com suas permissões próprias, persistência EF, migrations e catálogos WPF. Produto e Material também permanecem independentes. Nenhum vínculo automático entre Categoria, Unidade de Medida, Material, Produto ou Centro de Custo foi introduzido.

As salvaguardas de segurança anteriores permanecem cobertas: preservação da última capacidade efetiva de `roles.manage`, rollback conjunto da auditoria administrativa e compare-and-set de credencial na troca própria de senha.

O aviso conhecido no rollback de `AddAuthenticationThrottling` sobre `PRAGMA foreign_keys = 0` permanece dívida de migration já registrada. O rollback completa com sucesso; portanto, neste momento, ele não é classificado como bug funcional confirmado.

Pendências de smoke visual WPF, validação do diálogo fatal em build de diagnóstico e benchmark em hardware alvo são validações manuais/ambientais, não bugs confirmados.
