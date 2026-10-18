# Bugs

Nenhum bug funcional confirmado no baseline atual.

## Validação atual

O ERP é validado pelo `ERP Permanent CI` em Windows Server 2025 com .NET 10. O HEAD funcional mais recente concluiu com sucesso build Release, build da ferramenta de benchmark, 129/129 testes, verificação de snapshot EF e ciclo apply -> rollback 0 -> reapply das seis migrations.

A salvaguarda administrativa cobre remoção de associação usuário-role, desativação de role e revogação de `roles.manage`, sempre com base em capacidade efetiva. Os caminhos protegidos usam transação SQLite imediata para evitar corrida entre administradores concorrentes.

A auditoria administrativa possui testes de rollback para provar que falha do evento não deixa mutação persistida. Criação/status de role e grant/revogação de permissões também entram nesse contrato; operação bloqueada ou no-op não produz falso evento de sucesso.

A troca de senha pelo próprio usuário também possui persistência concorrente-segura. O `SqliteUserCredentialStore` revalida o hash esperado sob transação imediata; testes confirmam que hash obsoleto não altera estado, falha de auditoria reverte a troca e duas mudanças simultâneas baseadas na mesma credencial antiga resultam em exatamente um commit e um evento.

O catálogo de empresas foi integrado ao shell sem alteração de schema. O CI #102 compilou a nova tela WPF e validou os contratos `companies.read`/`companies.manage` com 65 testes de Application e 129 testes totais.

O run intermediário #94 da Parte F detectou incompatibilidade SQLite da primeira forma da migration de FKs; a implementação foi corrigida por reconstrução da tabela e os baselines seguintes fecharam verdes.

Pendências de validação manual ou de ambiente representativo, como smoke visual WPF e benchmark no hardware Windows alvo, são registradas no backlog/dívida técnica e não são classificadas como bugs confirmados.
