# Bugs

Nenhum bug funcional confirmado no baseline atual.

## Validação atual

O ERP é validado pelo `ERP Permanent CI` em Windows Server 2025 com .NET 10. O HEAD funcional mais recente concluiu com sucesso build Release, build da ferramenta de benchmark, 158/158 testes, verificação de snapshot EF e ciclo apply -> rollback 0 -> reapply das nove migrations.

O núcleo de Materiais está integrado com `materials.read/manage` na Application, persistência EF e migration `20261018222000_AddMaterials`. O CI #112 validou domínio, serviço, repositório, snapshot e migration smoke; a UI WPF de Materiais ainda não faz parte deste baseline.

As salvaguardas de segurança anteriores permanecem cobertas: preservação da última capacidade efetiva de `roles.manage`, rollback conjunto da auditoria administrativa e compare-and-set de credencial na troca própria de senha.

O aviso conhecido no rollback de `AddAuthenticationThrottling` sobre `PRAGMA foreign_keys = 0` permanece dívida de migration já registrada. O rollback completa com sucesso; portanto, neste momento, ele não é classificado como bug funcional confirmado.

Pendências de smoke visual WPF, validação do diálogo fatal em build de diagnóstico e benchmark em hardware alvo são validações manuais/ambientais, não bugs confirmados.
