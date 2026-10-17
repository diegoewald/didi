# Bugs

Nenhum bug funcional confirmado no baseline atual.

## Validação atual

O ERP é validado pelo `ERP Permanent CI` em Windows Server 2025 com .NET 10. O HEAD funcional mais recente concluiu com sucesso build Release, build da ferramenta de benchmark, 101/101 testes, verificação de snapshot EF e ciclo apply -> rollback 0 -> reapply das cinco migrations.

A Parte E inclui teste de persistência concorrente para a salvaguarda da última capacidade efetiva de `roles.manage`; duas remoções simultâneas dos dois últimos administradores preservam exatamente uma associação administrativa.

Pendências de validação manual ou de ambiente representativo, como smoke visual WPF e benchmark no hardware Windows alvo, são registradas no backlog/dívida técnica e não são classificadas como bugs confirmados.
