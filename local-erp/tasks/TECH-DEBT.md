# Dívida técnica

## Registrada

- O shell ainda possui estilos locais; extrair design system somente quando a repetição justificar.
- Backup/restauração e atualização possuem estratégia documentada, mas fluxo final de produto ainda não está implementado.
- Segurança, administração, cadastros-base e os cinco slices atuais de Compras — Solicitação, RFQ, Comparação, Adjudicação e Pedido de Compra — estão implementados e validados. Qualquer próximo slice funcional de Compras deve ser especificado antes de código.
- O tratamento global de exceções além do startup está implementado. Erros recuperáveis continuam responsabilidade do caso de uso/tela correspondente; não usar a barreira global como substituto de tratamento local.
- A proteção administrativa cobre os caminhos atuais que podem eliminar `roles.manage`; futuras mutações do grafo RBAC devem reutilizar salvaguarda equivalente.
- `SecurityAuditEvents` ainda não possui política aprovada de retenção/limpeza.
- Falhas de auditoria standalone em caminhos de login sem alteração de estado ainda propagam erro; avaliar política dedicada antes de release.
- A troca própria de senha mantém a sessão local atual; revogação distribuída só deve ser projetada quando existir infraestrutura de sessões multiusuário.
- `CompanyBranch` possui apenas empresa, nome e status. Dados fiscais, endereço, códigos internos, matriz/filial legal ou cascata de status continuam sem requisito definido e não devem ser inventados.
- `Supplier` possui apenas nome e status. CNPJ/CPF, endereço, contatos, dados bancários, condições comerciais e códigos externos devem depender de requisitos reais do fluxo de compras.
- `Material` possui apenas nome e status. Código/SKU, unidade, categoria, estoque, preço, fornecedor padrão ou outros vínculos comerciais não devem ser adicionados sem requisito explícito.
- `Product` possui apenas nome e status. Código/SKU, unidade, categoria, estoque, preço, tributação, composição/BOM e vínculos comerciais não devem ser adicionados sem requisito explícito.
- `MeasurementUnit` possui apenas nome e status. Sigla/símbolo, precisão decimal, fatores de conversão e vínculo com Produto/Material não devem ser adicionados sem requisito explícito.
- `Category` possui apenas nome e status. Hierarquia, categoria-pai, código e vínculo com Produto/Material não devem ser adicionados sem requisito explícito.
- `CostCenter` possui apenas nome e status. Código, hierarquia, vínculo obrigatório com empresa/filial, regra financeira e relacionamentos automáticos não devem ser adicionados sem requisito explícito.
- `Material` e `Product` permanecem conceitos separados; não unificar os dois nem criar relacionamento/composição apenas para reduzir número de cadastros.
- `materials.read/manage`, `products.read/manage`, `units.read/manage`, `categories.read/manage` e `cost-centers.read/manage` entram automaticamente no bootstrap de instalações novas. Roles de instalações existentes não recebem grants novos automaticamente; essa ausência de elevação implícita é deliberada e preserva menor privilégio.
- A repetição entre os catálogos WPF agora inclui Centros de Custo. Qualquer consolidação em componente comum deve ser feita em refactor próprio, após definir abstração que reduza complexidade sem misturar regras específicas.
- A migration `20261017234500_AddAdministrativeAuditContext` usa rebuild explícito de tabela por limitação SQLite; preservar esse padrão quando necessário.
- O rollback de `20261016131952_AddAuthenticationThrottling` ainda emite aviso do EF Core por executar `PRAGMA foreign_keys = 0` fora de transação. O rollback conclui com sucesso, mas a estratégia deve ser revisada antes de release sem alterar migration histórica de forma insegura.
- O CI Windows agora possui smoke WPF automatizado com banco descartável para bootstrap, autenticação, abertura do shell, navegação pelas abas de Compras e logout. Esse gate não valida ergonomia, foco, DPI nem o fluxo completo com dados e, portanto, não substitui o smoke visual/interativo manual em Windows real.
- Empresas, Filiais, Fornecedores, Materiais, Produtos, Unidades de Medida, Categorias, Centros de Custo, Solicitação de Compra, RFQ, Comparação, Adjudicação, Pedido de Compra e o encerramento por falha global precisam entrar no smoke manual completo.
- O benchmark de password hashing ainda deve ser repetido em hardware Windows representativo.
- A auditoria automatizada de dependências vulneráveis diretas/transitivas já integra o CI e falha para qualquer advisory conhecido; manter a fonte de auditoria acessível e revisar advisories novos continua parte da manutenção. O package/release portátil de QA já existe e é validado no CI, mas assinatura de código, Velopack e orquestração segura de upgrade de banco existente continuam fora dele e permanecem necessários para distribuição de produção.
- Não estender Compras para aprovação, recebimento, estoque, financeiro, fiscal ou integrações externas por suposição. Cada próximo slice deve definir estados, atores, invariantes e limites de escopo antes da implementação.

Nada acima deve ser ocultado por TODOs dispersos no código.
