# Bugs

Nenhum bug funcional confirmado no baseline atual.

## Validação atual

O ERP é validado pelo `ERP Permanent CI` em Windows Server 2025 com .NET 10. O commit documental `90a58dada08d306315d5b45e75163372136e6e6d` concluiu com sucesso build Release, build da ferramenta de benchmark, suíte automatizada, verificação de snapshot EF e ciclo apply -> rollback 0 -> reapply das migrations.

Pendências de validação manual ou de ambiente representativo, como smoke visual WPF e benchmark no hardware Windows alvo, são registradas no backlog/dívida técnica e não são classificadas como bugs confirmados.
