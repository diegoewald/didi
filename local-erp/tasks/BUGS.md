# Bugs

Nenhum bug funcional confirmado no baseline atual.

## Validação atual

O ERP é validado pelo `ERP Permanent CI` em Windows Server 2025 com .NET 10. O HEAD funcional de Filiais passou build Release, benchmark tool, 138/138 testes, snapshot EF e ciclo apply -> rollback 0 -> reapply das sete migrations.

`CompanyBranch` possui FK obrigatória para `Company` e teste real comprova rejeição de filial órfã. O serviço mantém `companies.read/manage` na Application e a UI apenas orquestra os casos de uso.

As salvaguardas de segurança anteriores permanecem cobertas: preservação da última capacidade efetiva de `roles.manage`, rollback conjunto da auditoria administrativa e compare-and-set de credencial na troca própria de senha.

O aviso conhecido no rollback de `AddAuthenticationThrottling` sobre `PRAGMA foreign_keys = 0` permanece dívida de migration já registrada; o rollback completa com sucesso e não foi introduzido pelo slice de Filiais.

Pendências de smoke visual WPF e benchmark em hardware alvo são validações manuais/ambientais, não bugs confirmados.
