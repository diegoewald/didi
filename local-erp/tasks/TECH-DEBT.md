# Dívida técnica

## Registrada

- O shell inicial possui estilos locais suficientes apenas para a fundação; o design system será extraído somente quando houver repetição real.
- Backup/restauração e atualização possuem estratégia documentada, mas o fluxo final de produto ainda não está implementado.
- O baseline de autenticação local e a administração de usuários Partes A–D estão implementados e validados; associação/remoção de roles, auditoria administrativa, troca de senha pelo próprio usuário e UI completa de roles/permissões ainda fazem parte do roadmap.
- O smoke visual/interativo do WPF em Windows real permanece obrigatório antes de release; compilação do XAML no CI não substitui validação de foco, DPI, aparência e interação.
- O benchmark de password hashing foi executado no Windows CI, mas ainda deve ser repetido em hardware Windows representativo do ambiente final antes de produção.
- `SecurityAuditEvents` ainda não possui política aprovada de retenção/limpeza. Não criar exclusão automática sem regra de negócio definida.
- Falhas de persistência da auditoria nos caminhos de conta inexistente, inativa ou já bloqueada atualmente propagam erro. O comportamento permanece fail-closed e não cria bypass de autenticação, mas acopla disponibilidade/UX desses caminhos ao audit store; avaliar política best-effort ou tratamento dedicado antes de release.

Nada acima deve ser ocultado por TODOs dispersos no código.
