# Bugs

Nenhum bug funcional confirmado no baseline atual.

## Validação atual

O ERP é validado pelo `ERP Permanent CI` em Windows Server 2025 com .NET 10. O HEAD funcional mais recente concluiu com sucesso build Release, build da ferramenta de benchmark, 117/117 testes, verificação de snapshot EF e ciclo apply -> rollback 0 -> reapply das seis migrations.

A salvaguarda administrativa cobre remoção de associação usuário-role, desativação de role e revogação de `roles.manage`, sempre com base em capacidade efetiva. Os caminhos protegidos usam transação SQLite imediata para evitar corrida entre administradores concorrentes.

A auditoria administrativa possui testes de rollback para provar que falha do evento não deixa mutação persistida. Criação/status de role e grant/revogação de permissões também entram nesse contrato; operação bloqueada ou no-op não produz falso evento de sucesso.

O run intermediário #94 da Parte F detectou incompatibilidade SQLite da primeira forma da migration de FKs; a implementação foi corrigida por reconstrução da tabela e os baselines seguintes fecharam verdes.

Pendências de validação manual ou de ambiente representativo, como smoke visual WPF e benchmark no hardware Windows alvo, são registradas no backlog/dívida técnica e não são classificadas como bugs confirmados.
