# Marco — Cadastro operacional de empresas

Data: 2026-10-17

## Escopo concluído

A Fase 6 começou pela menor fatia que já possuía domínio, Application e persistência reais: `Company`.

A navegação `Cadastros` agora abre uma tela própria de empresas em vez do conteúdo genérico do shell. O usuário pode:

- listar empresas ativas e inativas;
- atualizar a lista explicitamente;
- criar uma empresa por nome;
- renomear uma empresa existente;
- ativar uma empresa inativa;
- desativar uma empresa ativa com confirmação visual;
- ver estados de carregamento, erro e lista vazia.

## Autoridade e arquitetura

A UI não assume autorização nem regra de negócio.

- `CompanyService.ListAsync` exige `companies.read`;
- `CompanyService.CreateAsync` exige `companies.manage`;
- `CompanyService.RenameAsync` exige `companies.manage`;
- `CompanyService.SetActiveAsync` exige `companies.manage`;
- nome/status continuam validados pelo domínio `Company`;
- persistência continua em `EfCompanyRepository`.

`CompaniesCatalogViewModel` mantém apenas estado de apresentação/lista. As ações da View orquestram chamadas ao `CompanyService`, tratam feedback visual e atualizam a projeção local após sucesso.

## Banco de dados

Nenhuma alteração de modelo foi necessária. `Company` e seu mapping já existiam desde a fundação.

O CI confirmou:

- `has-pending-model-changes`: sem mudanças;
- 6 migrations;
- apply -> rollback 0 -> reapply: sucesso.

## Testes e CI

Dois testes de Application foram adicionados para tornar explícito o contrato de autorização do cadastro:

- listagem demanda `companies.read`;
- criação, renomeação e alteração de status demandam `companies.manage`.

Baseline funcional validado no `ERP Permanent CI` #102, run `39087653697`, commit `c45722e6469c0457fde0d07a19c2b1653d2e64f3`:

- Domain: 19/19;
- Application: 65/65;
- Persistence: 39/39;
- Infrastructure: 6/6;
- Total: 129/129;
- build Release: 0 warnings / 0 errors;
- ferramenta de benchmark: 0 warnings / 0 errors;
- snapshot EF consistente;
- ciclo completo das migrations aprovado.

## Limite deliberado

Este marco não cria um conceito de filial por suposição.

O modelo atual contém somente `Company` com identidade, nome e status. O próximo slice deve primeiro definir uma entidade de filial vinculada a `Company`, começando por um núcleo mínimo e tecnicamente seguro. Dados fiscais, endereço, códigos internos e regras específicas de matriz/filial dependem de requisito de negócio e não devem ser introduzidos sem definição.

## Próxima prioridade

Modelar filial vinculada a `Company`, adicionar persistência/testes e somente então criar a UI correspondente dentro de `Cadastros`.
