# Dívida técnica

## Registrada

- O shell ainda possui estilos locais; extrair design system somente quando a repetição justificar.
- Backup/restauração e atualização possuem estratégia documentada, mas fluxo final de produto ainda não está implementado.
- Segurança, administração, Empresa e Filial estão implementadas e validadas; a próxima frente funcional é Fornecedores.
- A proteção administrativa cobre os caminhos atuais que podem eliminar `roles.manage`; futuras mutações do grafo RBAC devem reutilizar salvaguarda equivalente.
- `SecurityAuditEvents` ainda não possui política aprovada de retenção/limpeza.
- Falhas de auditoria standalone em caminhos de login sem alteração de estado ainda propagam erro; avaliar política dedicada antes de release.
- A troca própria de senha mantém a sessão local atual; revogação distribuída só deve ser projetada quando existir infraestrutura de sessões multiusuário.
- `CompanyBranch` possui apenas empresa, nome e status. Dados fiscais, endereço, códigos internos, matriz/filial legal ou cascata de status continuam sem requisito definido e não devem ser inventados.
- A migration `20261017234500_AddAdministrativeAuditContext` usa rebuild explícito de tabela por limitação SQLite; preservar esse padrão quando necessário.
- O smoke visual/interativo do WPF em Windows real permanece obrigatório; compilação do XAML no CI não valida ergonomia, foco, DPI ou fluxo real.
- A tela de Empresas e a nova janela de Filiais precisam entrar no smoke manual, inclusive fechamento durante mutação e confirmações de desativação.
- O benchmark de password hashing ainda deve ser repetido em hardware Windows representativo.

Nada acima deve ser ocultado por TODOs dispersos no código.
