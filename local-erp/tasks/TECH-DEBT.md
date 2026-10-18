# Dívida técnica

## Registrada

- O shell inicial possui estilos locais suficientes apenas para a fundação; o design system será extraído somente quando houver repetição real.
- Backup/restauração e atualização possuem estratégia documentada, mas o fluxo final de produto ainda não está implementado.
- O baseline de autenticação local, a administração de usuários Partes A–F e a UI completa de roles/permissões estão implementados e validados; troca de senha pelo próprio usuário é a próxima frente funcional.
- A proteção administrativa cobre associação usuário-role, desativação de role e revogação de `roles.manage` com base em capacidade efetiva. Futuras formas de alterar o grafo RBAC deverão reutilizar salvaguarda equivalente antes de serem expostas.
- A auditoria atômica cobre as mutações administrativas atualmente expostas, inclusive criação/status de role e grant/revogação de permissões. Novas mutações críticas deverão entrar no mesmo contrato antes de UI/release.
- O smoke visual/interativo do WPF em Windows real permanece obrigatório antes de release; compilação do XAML no CI não substitui validação de foco, DPI, aparência e interação.
- O benchmark de password hashing foi executado no Windows CI, mas ainda deve ser repetido em hardware Windows representativo do ambiente final antes de produção.
- `SecurityAuditEvents` ainda não possui política aprovada de retenção/limpeza. Não criar exclusão automática sem regra de negócio definida.
- Falhas de persistência da auditoria nos caminhos de conta inexistente, inativa ou já bloqueada atualmente propagam erro. O comportamento permanece fail-closed e não cria bypass de autenticação, mas acopla disponibilidade/UX desses caminhos ao audit store; avaliar política best-effort ou tratamento dedicado antes de release.
- A migration `20261017234500_AddAdministrativeAuditContext` usa reconstrução explícita de `SecurityAuditEvents` para contornar a limitação do SQLite a `AddForeignKeyOperation` em tabela existente. O ciclo completo foi validado no CI; manter esse padrão em futuras alterações de FK SQLite que exijam rebuild.
- A janela de roles/permissões é compilada e coberta indiretamente pelos casos de uso, mas ainda depende de smoke manual para confirmar ergonomia, foco, DPI e fluxo de confirmações em Windows interativo.

Nada acima deve ser ocultado por TODOs dispersos no código.
