# Dívida técnica

## Registrada

- O shell ainda possui estilos locais; extrair design system somente quando a repetição justificar.
- Backup/restauração e atualização possuem estratégia documentada, mas fluxo final de produto ainda não está implementado.
- Segurança, administração, Empresa, Filial e Fornecedor estão implementados e validados; o núcleo de Material também está validado e a próxima frente funcional é sua UI WPF.
- O tratamento global de exceções além do startup está implementado. Erros recuperáveis continuam responsabilidade do caso de uso/tela correspondente; não usar a barreira global como substituto de tratamento local.
- A proteção administrativa cobre os caminhos atuais que podem eliminar `roles.manage`; futuras mutações do grafo RBAC devem reutilizar salvaguarda equivalente.
- `SecurityAuditEvents` ainda não possui política aprovada de retenção/limpeza.
- Falhas de auditoria standalone em caminhos de login sem alteração de estado ainda propagam erro; avaliar política dedicada antes de release.
- A troca própria de senha mantém a sessão local atual; revogação distribuída só deve ser projetada quando existir infraestrutura de sessões multiusuário.
- `CompanyBranch` possui apenas empresa, nome e status. Dados fiscais, endereço, códigos internos, matriz/filial legal ou cascata de status continuam sem requisito definido e não devem ser inventados.
- `Supplier` possui apenas nome e status. CNPJ/CPF, endereço, contatos, dados bancários, condições comerciais e códigos externos devem depender de requisitos reais do fluxo de compras.
- `Material` possui apenas nome e status. Código/SKU, unidade, categoria, estoque, preço, fornecedor padrão ou outros vínculos comerciais não devem ser adicionados sem requisito explícito.
- `Material` e Produto permanecem conceitos separados; não unificar os dois apenas para reduzir número de cadastros.
- `materials.read` e `materials.manage` entram automaticamente no bootstrap de instalações novas. Roles de instalações existentes não recebem grants novos automaticamente; essa ausência de elevação implícita é deliberada e preserva menor privilégio.
- A migration `20261017234500_AddAdministrativeAuditContext` usa rebuild explícito de tabela por limitação SQLite; preservar esse padrão quando necessário.
- O rollback de `20261016131952_AddAuthenticationThrottling` ainda emite aviso do EF Core por executar `PRAGMA foreign_keys = 0` fora de transação. O rollback conclui com sucesso, mas a estratégia deve ser revisada antes de release sem alterar migration histórica de forma insegura.
- O smoke visual/interativo do WPF em Windows real permanece obrigatório; compilação do XAML no CI não valida ergonomia, foco, DPI ou fluxo real.
- Empresas, Filiais, Fornecedores e o encerramento por falha global precisam entrar no smoke manual; Materiais deve ser incluído após sua UI ser criada.
- O benchmark de password hashing ainda deve ser repetido em hardware Windows representativo.
- Verificação automatizada de dependências vulneráveis e script de package/release permanecem pendências de engenharia.

Nada acima deve ser ocultado por TODOs dispersos no código.
